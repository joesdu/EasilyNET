using System.Security.Cryptography;
using EasilyNET.Mongo.AspNetCore.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS resumable upload helper - supports breakpoint resume for large file uploads</para>
///     <para xml:lang="zh">GridFS 断点续传辅助类 - 支持大文件上传的断点续传</para>
/// </summary>
public class GridFSResumableUploadHelper
{
    private readonly IGridFSBucket _bucket;
    private readonly IMongoCollection<BsonDocument> _chunksCollection;
    private readonly IMongoCollection<GridFSUploadSession> _sessionCollection;

    /// <summary>
    ///     <para xml:lang="en">Initialize resumable upload helper</para>
    ///     <para xml:lang="zh">初始化断点续传辅助类</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    public GridFSResumableUploadHelper(IGridFSBucket bucket)
    {
        _bucket = bucket;
        var database = _bucket.Database;
        _sessionCollection = database.GetCollection<GridFSUploadSession>("fs.upload_sessions");
        _chunksCollection = database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.chunks");

        // 创建索引以提升查询性能
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // 为会话集合创建索引
        var sessionIndexKeys = Builders<GridFSUploadSession>.IndexKeys
                                                            .Ascending(s => s.ExpiresAt)
                                                            .Ascending(s => s.Status);
        var sessionIndexModel = new CreateIndexModel<GridFSUploadSession>(sessionIndexKeys,
            new() { Name = "ExpiresAt_Status_Index", Background = true });
        _sessionCollection.Indexes.CreateOne(sessionIndexModel);

        // TTL 索引 - 自动清理过期会话
        var ttlIndexKeys = Builders<GridFSUploadSession>.IndexKeys.Ascending(s => s.ExpiresAt);
        var ttlIndexModel = new CreateIndexModel<GridFSUploadSession>(ttlIndexKeys,
            new() { Name = "TTL_Index", ExpireAfter = TimeSpan.Zero, Background = true });
        _sessionCollection.Indexes.CreateOne(ttlIndexModel);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a new resumable upload session. Returns session ID that can be used to resume upload later.
    ///     </para>
    ///     <para xml:lang="zh">创建新的断点续传会话。返回会话 ID,可用于稍后恢复上传。</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="totalSize">
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </param>
    /// <param name="metadata">
    ///     <para xml:lang="en">File metadata (optional)</para>
    ///     <para xml:lang="zh">文件元数据(可选)</para>
    /// </param>
    /// <param name="chunkSize">
    ///     <para xml:lang="en">Chunk size in bytes (optional, uses optimal size if not specified)</para>
    ///     <para xml:lang="zh">块大小(可选,未指定时使用最优大小)</para>
    /// </param>
    /// <param name="sessionExpirationHours">
    ///     <para xml:lang="en">Session expiration time in hours (default: 24 hours)</para>
    ///     <para xml:lang="zh">会话过期时间(小时,默认 24 小时)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Upload session</para>
    ///     <para xml:lang="zh">上传会话</para>
    /// </returns>
    public async Task<GridFSUploadSession> CreateSessionAsync(
        string filename,
        long totalSize,
        BsonDocument? metadata = null,
        int? chunkSize = null,
        int sessionExpirationHours = 24,
        CancellationToken cancellationToken = default)
    {
        var session = new GridFSUploadSession
        {
            SessionId = Guid.NewGuid().ToString("N"),
            Filename = filename,
            TotalSize = totalSize,
            UploadedSize = 0,
            ChunkSize = chunkSize ?? GridFSUploadHelper.GetOptimalChunkSize(totalSize),
            Metadata = metadata,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(sessionExpirationHours),
            Status = UploadStatus.InProgress
        };
        await _sessionCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Get upload session by session ID</para>
    ///     <para xml:lang="zh">通过会话 ID 获取上传会话</para>
    /// </summary>
    public async Task<GridFSUploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        return await _sessionCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Upload a chunk of data. Supports uploading chunks in any order (out-of-order upload).
    ///     </para>
    ///     <para xml:lang="zh">上传数据块。支持乱序上传(任意顺序上传块)。</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="chunkNumber">
    ///     <para xml:lang="en">Chunk number (0-based)</para>
    ///     <para xml:lang="zh">块编号(从 0 开始)</para>
    /// </param>
    /// <param name="data">
    ///     <para xml:lang="en">Chunk data</para>
    ///     <para xml:lang="zh">块数据</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Updated session</para>
    ///     <para xml:lang="zh">更新后的会话</para>
    /// </returns>
    public async Task<GridFSUploadSession> UploadChunkAsync(
        string sessionId,
        int chunkNumber,
        byte[] data,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found or expired");
        if (session.Status != UploadStatus.InProgress)
        {
            throw new InvalidOperationException($"Session {sessionId} is not in progress (status: {session.Status})");
        }

        // 检查块是否已上传
        if (session.UploadedChunks.Contains(chunkNumber))
        {
            return session; // 跳过已上传的块
        }

        // 临时存储块数据
        var chunkDoc = new BsonDocument
        {
            { "session_id", sessionId },
            { "n", chunkNumber },
            { "data", new BsonBinaryData(data) },
            { "uploadedAt", DateTime.UtcNow }
        };
        await _chunksCollection.InsertOneAsync(chunkDoc, cancellationToken: cancellationToken);

        // 更新会话
        session.UploadedChunks.Add(chunkNumber);
        session.UploadedChunks.Sort();
        session.UploadedSize += data.Length;
        session.UpdatedAt = DateTime.UtcNow;
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update
                                                  .Set(s => s.UploadedChunks, session.UploadedChunks)
                                                  .Set(s => s.UploadedSize, session.UploadedSize)
                                                  .Set(s => s.UpdatedAt, session.UpdatedAt);
        await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Finalize upload - combines all chunks into a complete GridFS file. Call this after all chunks are uploaded.
    ///     </para>
    ///     <para xml:lang="zh">完成上传 - 将所有块组合成完整的 GridFS 文件。在所有块上传完成后调用。</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="verifyHash">
    ///     <para xml:lang="en">Expected file hash for verification (optional)</para>
    ///     <para xml:lang="zh">用于验证的预期文件哈希值(可选)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">GridFS file ID</para>
    ///     <para xml:lang="zh">GridFS 文件 ID</para>
    /// </returns>
    public async Task<ObjectId> FinalizeUploadAsync(
        string sessionId,
        string? verifyHash = null,
        CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
        if (session.Status == UploadStatus.Completed)
        {
            return ObjectId.Parse(session.FileId!);
        }

        // 计算总块数
        var totalChunks = (int)Math.Ceiling((double)session.TotalSize / session.ChunkSize);

        // 检查是否所有块都已上传
        if (session.UploadedChunks.Count != totalChunks)
        {
            throw new InvalidOperationException($"Not all chunks uploaded. Expected: {totalChunks}, Got: {session.UploadedChunks.Count}");
        }

        // 从临时存储读取所有块并组合
        using var memoryStream = new MemoryStream((int)session.TotalSize);
        using var md5 = MD5.Create();
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
        var chunks = await _chunksCollection.Find(chunkFilter)
                                            .Sort(Builders<BsonDocument>.Sort.Ascending("n"))
                                            .ToListAsync(cancellationToken);
        foreach (var chunk in chunks)
        {
            var chunkData = chunk["data"].AsByteArray;
            await memoryStream.WriteAsync(chunkData, cancellationToken);
        }
        memoryStream.Position = 0;

        // 验证文件哈希(如果提供)
        if (!string.IsNullOrEmpty(verifyHash))
        {
            var computedHash = Convert.ToHexString(await md5.ComputeHashAsync(memoryStream, cancellationToken));
            if (!computedHash.Equals(verifyHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("File hash verification failed");
            }
            memoryStream.Position = 0;
        }

        // 上传到 GridFS
        var uploadOptions = new GridFSUploadOptions
        {
            ChunkSizeBytes = session.ChunkSize,
            Metadata = session.Metadata
        };
        var fileId = await _bucket.UploadFromStreamAsync(session.Filename, memoryStream, uploadOptions, cancellationToken);

        // 更新会话状态
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update
                                                  .Set(s => s.FileId, fileId.ToString())
                                                  .Set(s => s.Status, UploadStatus.Completed)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        // 清理临时块数据
        await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);
        return fileId;
    }

    /// <summary>
    ///     <para xml:lang="en">Get missing chunk numbers for a session</para>
    ///     <para xml:lang="zh">获取会话中缺失的块编号</para>
    /// </summary>
    public async Task<List<int>> GetMissingChunksAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
        var totalChunks = (int)Math.Ceiling((double)session.TotalSize / session.ChunkSize);
        var allChunks = Enumerable.Range(0, totalChunks).ToList();
        var missingChunks = allChunks.Except(session.UploadedChunks).ToList();
        return missingChunks;
    }

    /// <summary>
    ///     <para xml:lang="en">Cancel upload session and clean up temporary data</para>
    ///     <para xml:lang="zh">取消上传会话并清理临时数据</para>
    /// </summary>
    public async Task CancelSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update
                                                  .Set(s => s.Status, UploadStatus.Cancelled)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        // 清理临时块数据
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
        await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Clean up expired sessions (can be called periodically by a background job)</para>
    ///     <para xml:lang="zh">清理过期会话(可由后台任务定期调用)</para>
    /// </summary>
    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.And(Builders<GridFSUploadSession>.Filter.Lt(s => s.ExpiresAt, DateTime.UtcNow),
            Builders<GridFSUploadSession>.Filter.Ne(s => s.Status, UploadStatus.Completed));
        var expiredSessions = await _sessionCollection.Find(filter).ToListAsync(cancellationToken);
        foreach (var session in expiredSessions)
        {
            // 清理临时块数据
            var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", session.SessionId);
            await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);

            // 标记会话为过期
            var updateFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, session.SessionId);
            var update = Builders<GridFSUploadSession>.Update.Set(s => s.Status, UploadStatus.Expired);
            await _sessionCollection.UpdateOneAsync(updateFilter, update, cancellationToken: cancellationToken);
        }
    }
}