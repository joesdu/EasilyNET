using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">Manages upload session lifecycle - CRUD operations for GridFS upload sessions</para>
///     <para xml:lang="zh">管理上传会话生命周期 - GridFS 上传会话的 CRUD 操作</para>
/// </summary>
internal sealed class UploadSessionManager
{
    private readonly IGridFSBucket _bucket;
    private readonly ILogger _logger;

    /// <summary>
    ///     <para xml:lang="en">Initialize upload session manager</para>
    ///     <para xml:lang="zh">初始化上传会话管理器</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">Logger instance (optional)</para>
    ///     <para xml:lang="zh">日志记录器实例(可选)</para>
    /// </param>
    public UploadSessionManager(IGridFSBucket bucket, ILogger? logger = null)
    {
        _bucket = bucket;
        _logger = logger ?? NullLogger.Instance;
        SessionCollection = bucket.Database.GetCollection<GridFSUploadSession>(GridFSDefaults.UploadSessionCollectionName);
        CreateIndexes();
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the session collection for direct access</para>
    ///     <para xml:lang="zh">获取会话集合以便直接访问</para>
    /// </summary>
    private IMongoCollection<GridFSUploadSession> SessionCollection { get; }

    private void CreateIndexes()
    {
        // 为会话集合创建索引
        var sessionIndexKeys = Builders<GridFSUploadSession>.IndexKeys.Ascending(s => s.ExpiresAt).Ascending(s => s.Status);
        var sessionIndexModel = new CreateIndexModel<GridFSUploadSession>(sessionIndexKeys, new()
        {
            Name = "ExpiresAt_Status_Index",
            Background = true
        });
        SessionCollection.Indexes.CreateOne(sessionIndexModel);

        // TTL 索引 - 自动清理过期会话
        var ttlIndexKeys = Builders<GridFSUploadSession>.IndexKeys.Ascending(s => s.ExpiresAt);
        var ttlIndexModel = new CreateIndexModel<GridFSUploadSession>(ttlIndexKeys, new()
        {
            Name = "TTL_Index",
            ExpireAfter = TimeSpan.Zero,
            Background = true
        });
        SessionCollection.Indexes.CreateOne(ttlIndexModel);
    }

    /// <summary>
    ///     <para xml:lang="en">Create a new upload session</para>
    ///     <para xml:lang="zh">创建新的上传会话</para>
    /// </summary>
    public async Task<GridFSUploadSession> CreateSessionAsync(
        string filename,
        long totalSize,
        string? fileHash,
        string? contentType = null,
        int? chunkSize = null,
        int sessionExpirationHours = GridFSDefaults.DefaultSessionExpirationHours,
        CancellationToken cancellationToken = default)
    {
        // 验证分片大小
        if (chunkSize.HasValue && chunkSize.Value % GridFSDefaults.StandardChunkSize != 0)
        {
            throw new ArgumentException($"Chunk size must be a multiple of {GridFSDefaults.StandardChunkSize} bytes (2MB).", nameof(chunkSize));
        }

        // 1. 检查是否存在相同哈希的文件 (秒传)
        if (!string.IsNullOrEmpty(fileHash))
        {
            // 统一转换为大写进行比较,确保与存储的格式一致
            fileHash = fileHash.ToUpperInvariant();
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
            var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (existingFile != null)
            {
                _logger.LogInformation("Instant upload (deduplication) for file: {Filename}, hash: {FileHash}", filename, fileHash);
                // 增加引用计数
                await IncrementRefCountAsync(existingFile.Id, cancellationToken);
                var completedSession = new GridFSUploadSession
                {
                    SessionId = ObjectId.GenerateNewId().ToString(),
                    Filename = filename,
                    TotalSize = totalSize,
                    UploadedSize = totalSize,
                    ChunkSize = chunkSize ?? existingFile.ChunkSizeBytes,
                    ContentType = contentType,
                    FileId = existingFile.Id.ToString(),
                    FileHash = fileHash,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.MaxValue, // 永久保存
                    Status = UploadStatus.Completed,
                    UploadedChunks = []
                };
                await SessionCollection.InsertOneAsync(completedSession, cancellationToken: cancellationToken);
                return completedSession;
            }
        }

        // 2. 正常创建会话
        // 确保分片大小是 GridFSDefaults.StandardChunkSize (2MB) 的整数倍, 以便直接映射到 GridFS chunks
        var session = new GridFSUploadSession
        {
            SessionId = ObjectId.GenerateNewId().ToString(),
            Filename = filename,
            TotalSize = totalSize,
            UploadedSize = 0,
            ChunkSize = chunkSize ??
                        totalSize switch
                        {
                            < GridFSDefaults.SmallFileSizeThreshold  => 1 * GridFSDefaults.StandardChunkSize, // < 20MB: 2MB 分片
                            < GridFSDefaults.MediumFileSizeThreshold => 2 * GridFSDefaults.StandardChunkSize, // < 100MB: 4MB 分片
                            _                                        => 5 * GridFSDefaults.StandardChunkSize  // > 100MB: 10MB 分片
                        },
            ContentType = contentType,
            FileId = ObjectId.GenerateNewId().ToString(), // 预先生成 FileId
            FileHash = fileHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(sessionExpirationHours),
            Status = UploadStatus.InProgress
        };
        await SessionCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Get upload session by session ID</para>
    ///     <para xml:lang="zh">通过会话 ID 获取上传会话</para>
    /// </summary>
    public async Task<GridFSUploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var session = await SessionCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        session?.UploadedChunks.Sort();
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Update session with uploaded chunk information</para>
    ///     <para xml:lang="zh">更新会话的已上传块信息</para>
    /// </summary>
    public async Task UpdateSessionChunkAsync(string sessionId, int chunkNumber, int dataLength, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber)
                                                  .Inc(s => s.UploadedSize, dataLength)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow);
        await SessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Sync uploaded chunk to session (for duplicate chunk handling)</para>
    ///     <para xml:lang="zh">同步已上传块到会话(用于处理重复块)</para>
    /// </summary>
    public async Task SyncUploadedChunkAsync(string sessionId, int chunkNumber, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber);
        await SessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Mark session as completed</para>
    ///     <para xml:lang="zh">标记会话为已完成</para>
    /// </summary>
    public async Task UpdateSessionCompletedAsync(string sessionId, ObjectId fileId, string fileHash, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update
                                                  .Set(s => s.FileId, fileId.ToString())
                                                  .Set(s => s.FileHash, fileHash)
                                                  .Set(s => s.Status, UploadStatus.Completed)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow)
                                                  .Set(s => s.ExpiresAt, DateTime.MaxValue); // 设置为永不过期(持久化)
        await SessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        _logger.LogDebug("Session updated for sessionId: {SessionId}", sessionId);
    }

    /// <summary>
    ///     <para xml:lang="en">Cancel upload session</para>
    ///     <para xml:lang="zh">取消上传会话</para>
    /// </summary>
    public async Task CancelSessionAsync(string sessionId, bool deleteSession = true, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        if (deleteSession)
        {
            // 直接删除会话记录
            await SessionCollection.DeleteOneAsync(filter, cancellationToken);
        }
        else
        {
            // 仅标记为已取消状态(保留记录用于审计)
            var update = Builders<GridFSUploadSession>.Update.Set(s => s.Status, UploadStatus.Cancelled).Set(s => s.UpdatedAt, DateTime.UtcNow);
            await SessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Increment reference count for a file (for deduplication)</para>
    ///     <para xml:lang="zh">增加文件的引用计数(用于去重)</para>
    /// </summary>
    private async Task IncrementRefCountAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
        var update = Builders<BsonDocument>.Update.Inc("metadata.refCount", 1);
        await filesCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Try to find an existing file with the same hash (deduplication)</para>
    ///     <para xml:lang="zh">尝试查找具有相同哈希的现有文件(去重)</para>
    /// </summary>
    public async Task<GridFSFileInfo?> FindFileByHashAsync(string fileHash, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
        return await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
    }
}