using System.Diagnostics;
using System.Security.Cryptography;
using EasilyNET.Mongo.AspNetCore.Models;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS resumable upload helper - supports breakpoint resume for large file uploads</para>
///     <para xml:lang="zh">GridFS 断点续传辅助类 - 支持大文件上传的断点续传</para>
/// </summary>
public sealed class GridFSHelper
{
    private const int GridFSChunkSize = 261120; // 255KB - GridFS standard chunk size for optimal streaming
    private readonly IGridFSBucket _bucket;
    private readonly IMongoCollection<BsonDocument> _chunksCollection;
    private readonly GridFSRateLimitOptions _options;
    private readonly IMongoCollection<GridFSUploadSession> _sessionCollection;

    /// <summary>
    ///     <para xml:lang="en">Initialize resumable upload helper</para>
    ///     <para xml:lang="zh">初始化断点续传辅助类</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">Rate limit options (optional)</para>
    ///     <para xml:lang="zh">速率限制选项（可选）</para>
    /// </param>
    public GridFSHelper(IGridFSBucket bucket, IOptions<GridFSRateLimitOptions>? options = null)
    {
        _bucket = bucket;
        _options = options?.Value ?? new GridFSRateLimitOptions();
        var database = _bucket.Database;
        _sessionCollection = database.GetCollection<GridFSUploadSession>("fs.upload_sessions");
        // 直接操作 GridFS 的 chunks 集合
        _chunksCollection = database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.chunks");
        // 创建索引以提升查询性能
        CreateIndexes();
    }

    private void CreateIndexes()
    {
        // 为会话集合创建索引
        var sessionIndexKeys = Builders<GridFSUploadSession>.IndexKeys.Ascending(s => s.ExpiresAt).Ascending(s => s.Status);
        var sessionIndexModel = new CreateIndexModel<GridFSUploadSession>(sessionIndexKeys, new()
        {
            Name = "ExpiresAt_Status_Index",
            Background = true
        });
        _sessionCollection.Indexes.CreateOne(sessionIndexModel);

        // TTL 索引 - 自动清理过期会话
        var ttlIndexKeys = Builders<GridFSUploadSession>.IndexKeys.Ascending(s => s.ExpiresAt);
        var ttlIndexModel = new CreateIndexModel<GridFSUploadSession>(ttlIndexKeys, new()
        {
            Name = "TTL_Index",
            ExpireAfter = TimeSpan.Zero,
            Background = true
        });
        _sessionCollection.Indexes.CreateOne(ttlIndexModel);

        // 跨会话恢复索引 - 基于 fileHash + totalSize 查找未完成会话
        var resumeIndexKeys = Builders<GridFSUploadSession>.IndexKeys
                                                           .Ascending(s => s.FileHash)
                                                           .Ascending(s => s.TotalSize)
                                                           .Ascending(s => s.Status);
        var resumeIndexModel = new CreateIndexModel<GridFSUploadSession>(resumeIndexKeys, new()
        {
            Name = "FileHash_TotalSize_Status_Index",
            Background = true
        });
        _sessionCollection.Indexes.CreateOne(resumeIndexModel);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a new resumable upload session. Returns session ID that can be used to resume upload later.
    ///     If an incomplete session with the same fileHash and totalSize exists, it will be returned for resumption.
    ///     </para>
    ///     <para xml:lang="zh">创建新的断点续传会话。返回会话 ID,可用于稍后恢复上传。如果存在相同 fileHash 和 totalSize 的未完成会话,将返回该会话以便续传。</para>
    /// </summary>
    /// <param name="filename">
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </param>
    /// <param name="totalSize">
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </param>
    /// <param name="fileHash">
    ///     <para xml:lang="en">File SHA256</para>
    ///     <para xml:lang="zh">文件SHA256特征值</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">File content type (optional)</para>
    ///     <para xml:lang="zh">文件类型(可选)</para>
    /// </param>
    /// <param name="chunkSize">
    ///     <para xml:lang="en">Chunk size in bytes (optional, uses configured default if not specified)</para>
    ///     <para xml:lang="zh">块大小(可选,未指定时使用配置的默认值)</para>
    /// </param>
    /// <param name="sessionExpirationHours">
    ///     <para xml:lang="en">Session expiration time in hours (optional, uses configured default if not specified)</para>
    ///     <para xml:lang="zh">会话过期时间(小时,可选,未指定时使用配置的默认值)</para>
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
        string? fileHash,
        string? contentType = null,
        int? chunkSize = null,
        int? sessionExpirationHours = null,
        CancellationToken cancellationToken = default)
    {
        // 使用配置的默认值
        var effectiveChunkSize = chunkSize ?? _options.DefaultChunkSize;
        var effectiveExpirationHours = sessionExpirationHours ?? _options.SessionExpirationHours;

        // 验证分片大小在允许范围内
        if (effectiveChunkSize < _options.MinChunkSize || effectiveChunkSize > _options.MaxChunkSize)
        {
            throw new ArgumentException($"Chunk size must be between {_options.MinChunkSize} and {_options.MaxChunkSize} bytes.", nameof(chunkSize));
        }

        // 1. 检查是否存在相同哈希的已完成文件 (秒传)
        if (!string.IsNullOrEmpty(fileHash))
        {
            // 统一转换为大写进行比较,确保与存储的格式一致
            fileHash = fileHash.ToUpperInvariant();
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
            var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (existingFile != null)
            {
                Debug.WriteLine($"[INFO] Instant upload (deduplication) for file: {filename}, hash: {fileHash}");
                // 增加引用计数
                await IncrementRefCountAsync(existingFile.Id, cancellationToken);
                var completedSession = new GridFSUploadSession
                {
                    SessionId = ObjectId.GenerateNewId().ToString(),
                    Filename = filename,
                    TotalSize = totalSize,
                    UploadedSize = totalSize,
                    ChunkSize = effectiveChunkSize,
                    ContentType = contentType,
                    FileId = existingFile.Id.ToString(),
                    FileHash = fileHash,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.MaxValue, // 永久保存
                    Status = UploadStatus.Completed,
                    UploadedChunks = []
                };
                await _sessionCollection.InsertOneAsync(completedSession, cancellationToken: cancellationToken);
                return completedSession;
            }

            // 2. 检查是否存在未完成的会话可以恢复 (跨会话断点续传)
            if (_options.EnableCrossSessionResume)
            {
                var existingSession = await FindResumableSessionAsync(fileHash, totalSize, cancellationToken);
                if (existingSession != null)
                {
                    Debug.WriteLine($"[INFO] Found resumable session: {existingSession.SessionId} for file: {filename}, hash: {fileHash}");
                    // 更新会话的过期时间和文件名（可能用户重命名了文件）
                    var updateFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, existingSession.SessionId);
                    var update = Builders<GridFSUploadSession>.Update
                                                              .Set(s => s.Filename, filename)
                                                              .Set(s => s.ContentType, contentType ?? existingSession.ContentType)
                                                              .Set(s => s.UpdatedAt, DateTime.UtcNow)
                                                              .Set(s => s.ExpiresAt, DateTime.UtcNow.AddHours(effectiveExpirationHours));
                    await _sessionCollection.UpdateOneAsync(updateFilter, update, cancellationToken: cancellationToken);
                    // 返回更新后的会话
                    return (await GetSessionAsync(existingSession.SessionId, cancellationToken))!;
                }
            }
        }

        // 3. 正常创建新会话
        var session = new GridFSUploadSession
        {
            SessionId = ObjectId.GenerateNewId().ToString(),
            Filename = filename,
            TotalSize = totalSize,
            UploadedSize = 0,
            ChunkSize = effectiveChunkSize,
            ContentType = contentType,
            FileId = ObjectId.GenerateNewId().ToString(), // 预先生成 FileId
            FileHash = fileHash,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(effectiveExpirationHours),
            Status = UploadStatus.InProgress
        };
        await _sessionCollection.InsertOneAsync(session, cancellationToken: cancellationToken);
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Find a resumable session by file hash and total size</para>
    ///     <para xml:lang="zh">通过文件哈希和总大小查找可恢复的会话</para>
    /// </summary>
    /// <param name="fileHash">
    ///     <para xml:lang="en">File SHA256 hash</para>
    ///     <para xml:lang="zh">文件 SHA256 哈希</para>
    /// </param>
    /// <param name="totalSize">
    ///     <para xml:lang="en">Total file size in bytes</para>
    ///     <para xml:lang="zh">文件总大小(字节)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Resumable session or null if not found</para>
    ///     <para xml:lang="zh">可恢复的会话,如果未找到则返回 null</para>
    /// </returns>
    public async Task<GridFSUploadSession?> FindResumableSessionAsync(string fileHash, long totalSize, CancellationToken cancellationToken = default)
    {
        var normalizedHash = fileHash.ToUpperInvariant();
        var filter = Builders<GridFSUploadSession>.Filter.And(Builders<GridFSUploadSession>.Filter.Eq(s => s.FileHash, normalizedHash),
            Builders<GridFSUploadSession>.Filter.Eq(s => s.TotalSize, totalSize),
            Builders<GridFSUploadSession>.Filter.Eq(s => s.Status, UploadStatus.InProgress),
            Builders<GridFSUploadSession>.Filter.Gt(s => s.ExpiresAt, DateTime.UtcNow));
        var session = await _sessionCollection.Find(filter)
                                              .SortByDescending(s => s.UpdatedAt)
                                              .FirstOrDefaultAsync(cancellationToken);
        session?.UploadedChunks.Sort();
        return session;
    }

    /// <summary>
    ///     <para xml:lang="en">Get upload session by session ID</para>
    ///     <para xml:lang="zh">通过会话 ID 获取上传会话</para>
    /// </summary>
    public async Task<GridFSUploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var session = await _sessionCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        session?.UploadedChunks.Sort();
        return session;
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
    /// <param name="chunkHash">
    ///     <para xml:lang="en">Chunk SHA256 hash</para>
    ///     <para xml:lang="zh">块 SHA256 哈希</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Updated session</para>
    ///     <para xml:lang="zh">更新后的会话</para>
    /// </returns>
    public async Task<GridFSUploadSession> UploadChunkAsync(string sessionId, int chunkNumber, byte[] data, string chunkHash, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found or expired");
        if (session.Status != UploadStatus.InProgress)
        {
            throw new InvalidOperationException($"Session {sessionId} is not in progress (status: {session.Status})");
        }

        // 检查会话是否已过期
        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            throw new InvalidOperationException($"Session {sessionId} has expired");
        }

        // 计算该块在文件中的字节偏移量
        var byteOffset = (long)chunkNumber * session.ChunkSize;
        // 计算该块对应的第一个 GridFS 子块索引
        var baseGridFSChunkIndex = (int)(byteOffset / GridFSChunkSize);
        var fileId = ObjectId.Parse(session.FileId!);

        // 检查块是否已上传 - 从数据库中查询而不是依赖内存中的 session.UploadedChunks
        var chunkExistsFilter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("files_id", fileId),
            Builders<BsonDocument>.Filter.Eq("n", baseGridFSChunkIndex));
        var existingChunk = await _chunksCollection.Find(chunkExistsFilter).FirstOrDefaultAsync(cancellationToken);
        if (existingChunk is not null)
        {
            // 块已存在,同步 session.UploadedChunks 并返回
            if (session.UploadedChunks.Contains(chunkNumber))
            {
                return await GetSessionAsync(sessionId, cancellationToken) ?? session;
            }
            var syncFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
            var syncUpdate = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber);
            await _sessionCollection.UpdateOneAsync(syncFilter, syncUpdate, cancellationToken: cancellationToken);
            return await GetSessionAsync(sessionId, cancellationToken) ?? session;
        }

        // 验证块哈希（如果启用）
        if (_options.EnableChunkHashVerification)
        {
            var computedHashBytes = SHA256.HashData(data);
            var computedHash = Convert.ToHexString(computedHashBytes);
            if (!computedHash.Equals(chunkHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Chunk hash verification failed. Expected: {chunkHash}, Got: {computedHash}");
            }
        }

        // 直接写入 GridFS chunks 集合
        // 将上传的块拆分为 GridFS 标准块 (255KB)
        var subChunks = new List<BsonDocument>();
        var currentByteOffset = byteOffset;
        for (var i = 0; i < data.Length; i += GridFSChunkSize)
        {
            var length = Math.Min(GridFSChunkSize, data.Length - i);
            var subChunkData = new byte[length];
            Array.Copy(data, i, subChunkData, 0, length);
            // 使用字节偏移计算 GridFS 块索引
            var n = (int)(currentByteOffset / GridFSChunkSize);
            var chunkDoc = new BsonDocument
            {
                { "files_id", fileId },
                { "n", n },
                { "data", new BsonBinaryData(subChunkData) }
            };
            subChunks.Add(chunkDoc);
            currentByteOffset += length;
        }
        try
        {
            // 批量插入子块
            if (subChunks.Count > 0)
            {
                await _chunksCollection.InsertManyAsync(subChunks, cancellationToken: cancellationToken);
            }
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            // 如果发生重复键异常,说明该块已经存在(并发上传)
            // 同步会话状态并返回
            var retryFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
            var retryUpdate = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber);
            await _sessionCollection.UpdateOneAsync(retryFilter, retryUpdate, cancellationToken: cancellationToken);
            return await GetSessionAsync(sessionId, cancellationToken) ?? session;
        }

        // 更新会话 - 添加块号到 UploadedChunks 列表，并延长过期时间
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber)
                                                  .Inc(s => s.UploadedSize, data.Length)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow)
                                                  .Set(s => s.ExpiresAt, DateTime.UtcNow.AddHours(_options.SessionExpirationHours));
        await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        // 获取最新会话状态返回
        var updatedSession = await GetSessionAsync(sessionId, cancellationToken);
        return updatedSession ?? session;
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
    ///     <para xml:lang="en">Expected file hash (SHA256) for verification (optional)</para>
    ///     <para xml:lang="zh">用于验证的预期文件哈希值(SHA256)(可选)</para>
    /// </param>
    /// <param name="skipHashValidation">
    ///     <para xml:lang="en">Skip full hash validation (trust client hash, faster but less safe)</para>
    ///     <para xml:lang="zh">跳过服务器端全量哈希校验(依赖客户端哈希,更快但安全性降低)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">GridFS file ID</para>
    ///     <para xml:lang="zh">GridFS 文件 ID</para>
    /// </returns>
    public async Task<ObjectId> FinalizeUploadAsync(string sessionId, string? verifyHash = null, bool skipHashValidation = false, CancellationToken cancellationToken = default)
    {
        try
        {
            Debug.WriteLine($"[DEBUG] FinalizeUploadAsync started for session: {sessionId}");
            var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
            if (session.Status == UploadStatus.Completed)
            {
                return ObjectId.Parse(session.FileId!);
            }
            // Use client-provided hash if available, otherwise fall back to session hash
            var expectedHash = (verifyHash ?? session.FileHash)?.ToUpperInvariant();
            // 1. 尝试通过 verifyHash 进行去重 (如果提供了 hash)
            if (!string.IsNullOrEmpty(expectedHash))
            {
                var existingId = await TryDeduplicateAsync(sessionId, ObjectId.Parse(session.FileId!), expectedHash, cancellationToken);
                if (existingId != ObjectId.Empty)
                {
                    return existingId;
                }
            }

            // 计算总块数
            var totalChunks = (int)Math.Ceiling((double)session.TotalSize / session.ChunkSize);
            Debug.WriteLine($"[DEBUG] Expected chunks: {totalChunks}, TotalSize: {session.TotalSize}, ChunkSize: {session.ChunkSize}");
            // 从数据库中查询实际上传的块,而不是依赖 session.UploadedChunks
            var fileIdObj = ObjectId.Parse(session.FileId!);
            var chunkFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileIdObj);
            var uploadedChunkNumbers = await _chunksCollection.Find(chunkFilter).Project(Builders<BsonDocument>.Projection.Include("n")).ToListAsync(cancellationToken);

            // 使用字节偏移计算来映射 GridFS 块索引回上传块索引（与 GetMissingChunksAsync 保持一致）
            var actualUploadedChunks = uploadedChunkNumbers.Select(doc =>
            {
                var gridFSChunkIndex = doc["n"].AsInt32;
                // 计算该 GridFS 块对应的字节偏移
                var byteOffset = (long)gridFSChunkIndex * GridFSChunkSize;
                // 计算对应的上传块索引
                return (int)(byteOffset / session.ChunkSize);
            }).ToHashSet();
            Debug.WriteLine($"[DEBUG] Actual uploaded chunks (mapped): [{string.Join(", ", actualUploadedChunks.OrderBy(n => n))}]");

            // 检查是否所有块都已上传
            if (actualUploadedChunks.Count != totalChunks)
            {
                var missingChunks = Enumerable.Range(0, totalChunks).Except(actualUploadedChunks).ToList();
                throw new InvalidOperationException($"""
                                                     Not all chunks uploaded. 
                                                     Expected: {totalChunks}, 
                                                     Got: {actualUploadedChunks.Count}. 
                                                     Missing chunks: [{string.Join(", ", missingChunks)}]
                                                     """);
            }

            // 验证块号的连续性
            for (var i = 0; i < totalChunks; i++)
            {
                if (!actualUploadedChunks.Contains(i))
                {
                    throw new InvalidOperationException($"Missing chunk number {i}");
                }
            }
            Debug.WriteLine("[DEBUG] All chunks validated, creating GridFS file entry");

            // 优化: 直接创建 fs.files 文档, 避免读取和重写所有块
            // 注意: 如果需要严格的 SHA256 校验, 仍然需要读取所有块来计算哈希
            // 这里我们假设如果 verifyHash 为空, 或者为了性能, 我们跳过全量哈希计算
            // 如果提供了 verifyHash, 我们仍然需要计算哈希 (这会很慢, 但比读+写快)
            string computedHash;
            if (!string.IsNullOrEmpty(expectedHash))
            {
                if (skipHashValidation)
                {
                    computedHash = expectedHash;
                }
                else
                {
                    Debug.WriteLine("[DEBUG] Calculating hash for verification...");
                    using var sha256 = SHA256.Create();
                    var sort = Builders<BsonDocument>.Sort.Ascending("n");
                    var cursor = await _chunksCollection.Find(Builders<BsonDocument>.Filter.Eq("files_id", fileIdObj))
                                                        .Sort(sort)
                                                        .ToCursorAsync(cancellationToken);
                    while (await cursor.MoveNextAsync(cancellationToken))
                    {
                        foreach (var data in cursor.Current.Select(chunk => chunk["data"].AsBsonBinaryData.Bytes))
                        {
                            sha256.TransformBlock(data, 0, data.Length, null, 0);
                        }
                    }
                    sha256.TransformFinalBlock([], 0, 0);
                    computedHash = Convert.ToHexString(sha256.Hash!);
                    if (!computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException($"File hash verification failed. Expected: {expectedHash}, Got: {computedHash}");
                    }
                }
            }
            else
            {
                computedHash = string.Empty;
            }
            var fileId = ObjectId.Parse(session.FileId!);

            // 2. 再次尝试去重 (防止并发或 verifyHash 为空的情况)
            if (!string.IsNullOrEmpty(computedHash))
            {
                var dupFilter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", computedHash);
                var existingFile = await (await _bucket.FindAsync(dupFilter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
                if (existingFile != null)
                {
                    Debug.WriteLine($"[INFO] Duplicate file found after upload: {existingFile.Id}. Deleting new file {fileId}...");
                    // 删除刚才上传的冗余文件 (chunks)
                    await _bucket.DeleteAsync(fileId, cancellationToken);

                    // 增加现有文件的引用计数
                    await IncrementRefCountAsync(existingFile.Id, cancellationToken);

                    // 更新会话
                    await UpdateSessionCompletedAsync(sessionId, existingFile.Id, computedHash, cancellationToken);
                    return existingFile.Id;
                }
            }

            // 3. 创建 fs.files 文档
            var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
            var fileDoc = new BsonDocument
            {
                { "_id", fileId },
                { "length", session.TotalSize },
                { "chunkSize", GridFSChunkSize },
                { "uploadDate", DateTime.UtcNow },
                { "filename", session.Filename },
                { "contentType", session.ContentType is null ? BsonNull.Value : new BsonString(session.ContentType) },
                { "metadata", new BsonDocument() }
            };

            // 添加自定义元数据
            if (!fileDoc["metadata"].AsBsonDocument.Contains("fileHash"))
            {
                fileDoc["metadata"].AsBsonDocument.Add("fileHash", computedHash);
            }
            if (!fileDoc["metadata"].AsBsonDocument.Contains("refCount"))
            {
                fileDoc["metadata"].AsBsonDocument.Add("refCount", 1);
            }
            // 将 contentType 也保存到 metadata 中，便于后续读取
            if (!string.IsNullOrEmpty(session.ContentType) && !fileDoc["metadata"].AsBsonDocument.Contains("contentType"))
            {
                fileDoc["metadata"].AsBsonDocument.Add("contentType", session.ContentType);
            }
            await filesCollection.InsertOneAsync(fileDoc, cancellationToken: cancellationToken);

            // 更新会话
            await UpdateSessionCompletedAsync(sessionId, fileId, computedHash, cancellationToken);
            Debug.WriteLine("[DEBUG] FinalizeUploadAsync completed successfully");
            return fileId;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR] FinalizeUploadAsync failed: {ex.GetType().Name}");
            Debug.WriteLine($"[ERROR] Message: {ex.Message}");
            Debug.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
            }
            throw;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get missing chunk numbers for a session</para>
    ///     <para xml:lang="zh">获取会话中缺失的块编号</para>
    /// </summary>
    public async Task<List<int>> GetMissingChunksAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
        var totalChunks = (int)Math.Ceiling((double)session.TotalSize / session.ChunkSize);
        // 从数据库中查询实际上传的块
        var fileIdObj = ObjectId.Parse(session.FileId!);
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileIdObj);
        var uploadedChunkNumbers = await _chunksCollection.Find(chunkFilter).Project(Builders<BsonDocument>.Projection.Include("n")).ToListAsync(cancellationToken);

        // 使用字节偏移计算来映射 GridFS 块索引回上传块索引
        var actualUploadedChunks = uploadedChunkNumbers.Select(doc =>
        {
            var gridFSChunkIndex = doc["n"].AsInt32;
            // 计算该 GridFS 块对应的字节偏移
            var byteOffset = (long)gridFSChunkIndex * GridFSChunkSize;
            // 计算对应的上传块索引
            return (int)(byteOffset / session.ChunkSize);
        }).ToHashSet();
        var allChunks = Enumerable.Range(0, totalChunks).ToList();
        var missingChunks = allChunks.Where(n => !actualUploadedChunks.Contains(n)).ToList();
        return missingChunks;
    }

    /// <summary>
    ///     <para xml:lang="en">Cancel upload session and clean up temporary data</para>
    ///     <para xml:lang="zh">取消上传会话并清理临时数据</para>
    /// </summary>
    /// <param name="sessionId">
    ///     <para xml:lang="en">Session ID</para>
    ///     <para xml:lang="zh">会话 ID</para>
    /// </param>
    /// <param name="deleteSession">
    ///     <para xml:lang="en">Whether to delete the session record (default: true). If false, only marks as cancelled.</para>
    ///     <para xml:lang="zh">是否删除会话记录(默认: true)。如果为 false,仅标记为已取消。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    public async Task CancelSessionAsync(string sessionId, bool deleteSession = true, CancellationToken cancellationToken = default)
    {
        var session = await GetSessionAsync(sessionId, cancellationToken);
        if (session != null && !string.IsNullOrEmpty(session.FileId))
        {
            await CleanupTempDataAsync(ObjectId.Parse(session.FileId), cancellationToken);
        }
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        if (deleteSession)
        {
            // 直接删除会话记录
            await _sessionCollection.DeleteOneAsync(filter, cancellationToken);
        }
        else
        {
            // 仅标记为已取消状态(保留记录用于审计)
            var update = Builders<GridFSUploadSession>.Update.Set(s => s.Status, UploadStatus.Cancelled).Set(s => s.UpdatedAt, DateTime.UtcNow);
            await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete file with reference counting</para>
    ///     <para xml:lang="zh">带引用计数的删除文件</para>
    /// </summary>
    private async Task DeleteFileAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);

        // 原子操作: 减少引用计数并返回更新后的文档
        var update = Builders<BsonDocument>.Update.Inc("metadata.refCount", -1);
        var options = new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After };
        var result = await filesCollection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (result != null)
        {
            var refCount = result.Contains("metadata") && result["metadata"].IsBsonDocument && result["metadata"].AsBsonDocument.Contains("refCount")
                               ? result["metadata"]["refCount"].AsInt32
                               : -1;
            if (refCount <= 0)
            {
                await _bucket.DeleteAsync(fileId, cancellationToken);
                Debug.WriteLine($"[INFO] File {fileId} deleted (refCount: {refCount})");
            }
            else
            {
                Debug.WriteLine($"[INFO] File {fileId} refCount decremented to {refCount}");
            }
        }
        else
        {
            Debug.WriteLine($"[WARN] File {fileId} not found during delete");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Downloads a full seekable stream from a GridFS file. Used with ASP.NET Core's built-in Range processing.
    ///     </para>
    ///     <para xml:lang="zh">从 GridFS 文件下载完整的可定位流。配合 ASP.NET Core 内置的 Range 处理使用。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">File ObjectId</para>
    ///     <para xml:lang="zh">文件 ObjectId</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Seekable stream with file info</para>
    ///     <para xml:lang="zh">可定位流及文件信息</para>
    /// </returns>
    public async Task<(Stream Stream, GridFSFileInfo FileInfo)> DownloadFullStreamAsync(ObjectId id, CancellationToken cancellationToken = default)
    {
        // 获取文件信息
        var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, id), cancellationToken: cancellationToken))
                           .FirstOrDefaultAsync(cancellationToken) ??
                       throw new FileNotFoundException($"File with ID {id} not found");
        // 打开可定位的下载流 - ASP.NET Core 的 enableRangeProcessing 需要可定位的流
        var stream = await _bucket.OpenDownloadStreamAsync(id, new() { Seekable = true }, cancellationToken);
        return (stream, fileInfo);
    }

    /// <summary>
    /// Rename a file in GridFS
    /// </summary>
    /// <param name="id"></param>
    /// <param name="newName"></param>
    /// <param name="cancellationToken"></param>
    public async Task Rename(string id, string newName, CancellationToken cancellationToken = default)
    {
        await _bucket.RenameAsync(ObjectId.Parse(id), newName, cancellationToken);
    }

    /// <summary>
    /// Delete files by IDs with reference counting
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<IEnumerable<string>> Delete(string[] ids, CancellationToken cancellationToken = default)
    {
        var oids = ids.Select(ObjectId.Parse).ToList();
        var fi = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.In(c => c.Id, oids), cancellationToken: cancellationToken)).ToListAsync(cancellationToken);
        var fids = fi.Select(c => new { Id = c.Id.ToString(), FileName = c.Filename }).ToArray();
        // 删除 GridFS 中的文件 (使用引用计数删除)
        foreach (var item in fids)
        {
            await DeleteFileAsync(ObjectId.Parse(item.Id), cancellationToken);
        }
        return fids.Select(c => c.FileName);
    }

    private async Task IncrementRefCountAsync(ObjectId fileId, CancellationToken cancellationToken)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
        var update = Builders<BsonDocument>.Update.Inc("metadata.refCount", 1);
        await filesCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private async Task UpdateSessionCompletedAsync(string sessionId, ObjectId fileId, string fileHash, CancellationToken cancellationToken)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update
                                                  .Set(s => s.FileId, fileId.ToString())
                                                  .Set(s => s.FileHash, fileHash)
                                                  .Set(s => s.Status, UploadStatus.Completed)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow)
                                                  .Set(s => s.ExpiresAt, DateTime.MaxValue); // 设置为永不过期(持久化)
        await _sessionCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        Debug.WriteLine("[DEBUG] Session updated");
    }

    private async Task CleanupTempDataAsync(ObjectId fileId, CancellationToken cancellationToken)
    {
        // 清理临时块数据
        var deleteFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileId);
        await _chunksCollection.DeleteManyAsync(deleteFilter, cancellationToken);
        Debug.WriteLine($"[DEBUG] Temporary chunks cleaned up for fileId: {fileId}");
    }

    private async Task<ObjectId> TryDeduplicateAsync(string sessionId, ObjectId originalFileId, string fileHash, CancellationToken cancellationToken)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
        var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
        if (existingFile == null)
        {
            return ObjectId.Empty;
        }
        Debug.WriteLine($"[INFO] Duplicate file found (pre-check): {existingFile.Id}");
        await IncrementRefCountAsync(existingFile.Id, cancellationToken);
        await UpdateSessionCompletedAsync(sessionId, existingFile.Id, fileHash, cancellationToken);
        await CleanupTempDataAsync(originalFileId, cancellationToken);
        return existingFile.Id;
    }
}