using System.Diagnostics;
using System.Security.Cryptography;
using EasilyNET.Mongo.AspNetCore.Models;
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
    private const int GridFSChunkSize = 2 * 1024 * 1024; // 2MB standard chunk size
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
    public GridFSHelper(IGridFSBucket bucket)
    {
        _bucket = bucket;
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
    /// <param name="fileHash">
    ///     <para xml:lang="en">File SHA256</para>
    ///     <para xml:lang="zh">文件SHA256特征值</para>
    /// </param>
    /// <param name="contentType">
    ///     <para xml:lang="en">File content type (optional)</para>
    ///     <para xml:lang="zh">文件类型(可选)</para>
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
        string? fileHash,
        string? contentType = null,
        int? chunkSize = null,
        int sessionExpirationHours = 24,
        CancellationToken cancellationToken = default)
    {
        // 验证分片大小
        if (chunkSize.HasValue && chunkSize.Value % GridFSChunkSize != 0)
        {
            throw new ArgumentException($"Chunk size must be a multiple of {GridFSChunkSize} bytes (2MB).", nameof(chunkSize));
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
                Debug.WriteLine($"[INFO] Instant upload (deduplication) for file: {filename}, hash: {fileHash}");
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
                await _sessionCollection.InsertOneAsync(completedSession, cancellationToken: cancellationToken);
                return completedSession;
            }
        }

        // 2. 正常创建会话
        // 确保分片大小是 GridFSChunkSize (2MB) 的整数倍, 以便直接映射到 GridFS chunks
        var session = new GridFSUploadSession
        {
            SessionId = ObjectId.GenerateNewId().ToString(),
            Filename = filename,
            TotalSize = totalSize,
            UploadedSize = 0,
            ChunkSize = chunkSize ??
                        totalSize switch
                        {
                            < 20 * 1024 * 1024  => 1 * GridFSChunkSize, // < 20MB: 2MB 分片
                            < 100 * 1024 * 1024 => 2 * GridFSChunkSize, // < 100MB: 4MB 分片
                            _                   => 5 * GridFSChunkSize  // > 100MB: 10MB 分片
                        },
            ContentType = contentType,
            FileId = ObjectId.GenerateNewId().ToString(), // 预先生成 FileId
            FileHash = fileHash,
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
        // 检查块是否已上传 - 从数据库中查询而不是依赖内存中的 session.UploadedChunks
        // 注意: 由于我们现在直接写入 fs.chunks, 这里的 n 是 GridFS 的块索引, 而不是上传分片的索引
        // 一个上传分片可能对应多个 GridFS 块
        var chunksPerUpload = session.ChunkSize / GridFSChunkSize;
        var baseN = chunkNumber * chunksPerUpload;
        var fileId = ObjectId.Parse(session.FileId!);
        // 只要检查第一个子块是否存在即可
        var chunkExistsFilter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("files_id", fileId), Builders<BsonDocument>.Filter.Eq("n", baseN));
        var existingChunk = await _chunksCollection.Find(chunkExistsFilter).FirstOrDefaultAsync(cancellationToken);
        if (existingChunk is not null)
        {
            // 块已存在,同步 session.UploadedChunks 并返回
            // ReSharper disable once InvertIf
            if (!session.UploadedChunks.Contains(chunkNumber))
            {
                var syncFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
                var syncUpdate = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber);
                await _sessionCollection.UpdateOneAsync(syncFilter, syncUpdate, cancellationToken: cancellationToken);
            }
            return await GetSessionAsync(sessionId, cancellationToken) ?? session;
        }
        // 验证块哈希
        var computedHashBytes = SHA256.HashData(data);
        var computedHash = Convert.ToHexString(computedHashBytes);
        if (!computedHash.Equals(chunkHash, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Chunk hash verification failed. Expected: {chunkHash}, Got: {computedHash}");
        }

        // 直接写入 GridFS chunks 集合
        // 将上传的大块拆分为 GridFS 标准块 (2MB)
        var baseChunkIndex = chunkNumber * (session.ChunkSize / GridFSChunkSize);
        var subChunks = new List<BsonDocument>();
        for (var i = 0; i < data.Length; i += GridFSChunkSize)
        {
            var length = Math.Min(GridFSChunkSize, data.Length - i);
            var subChunkData = new byte[length];
            Array.Copy(data, i, subChunkData, 0, length);
            var n = baseChunkIndex + (i / GridFSChunkSize);
            var chunkDoc = new BsonDocument
            {
                { "files_id", fileId },
                { "n", n },
                { "data", new BsonBinaryData(subChunkData) }
            };
            subChunks.Add(chunkDoc);
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

        // 更新会话 - 添加块号到 UploadedChunks 列表
        var filter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, sessionId);
        var update = Builders<GridFSUploadSession>.Update.AddToSet(s => s.UploadedChunks, chunkNumber)
                                                  .Inc(s => s.UploadedSize, data.Length)
                                                  .Set(s => s.UpdatedAt, DateTime.UtcNow);
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

            // 将 GridFS 块索引 (n) 映射回上传分片索引
            var chunksPerUpload = Math.Max(1, session.ChunkSize / GridFSChunkSize);
            var actualUploadedChunks = uploadedChunkNumbers
                                       .Select(doc => doc["n"].AsInt32 / chunksPerUpload)
                                       .Distinct()
                                       .OrderBy(n => n)
                                       .ToList();
            Debug.WriteLine($"[DEBUG] Actual uploaded chunks (mapped): [{string.Join(", ", actualUploadedChunks)}]");

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
        var chunksPerUpload = Math.Max(1, session.ChunkSize / GridFSChunkSize);
        var actualUploadedChunks = uploadedChunkNumbers
                                   .Select(doc => doc["n"].AsInt32 / chunksPerUpload)
                                   .ToHashSet();
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
    ///     Downloads a range of bytes from a GridFS file. Supports HTTP Range header for video/audio streaming.
    ///     </para>
    ///     <para xml:lang="zh">从 GridFS 文件中下载指定范围的字节。支持 HTTP Range 头,用于视频/音频流传输。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">File ObjectId</para>
    ///     <para xml:lang="zh">文件 ObjectId</para>
    /// </param>
    /// <param name="startByte">
    ///     <para xml:lang="en">Start byte position (inclusive)</para>
    ///     <para xml:lang="zh">起始字节位置(包含)</para>
    /// </param>
    /// <param name="endByte">
    ///     <para xml:lang="en">End byte position (inclusive), null for end of file</para>
    ///     <para xml:lang="zh">结束字节位置(包含),null 表示文件末尾</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Range stream with file info</para>
    ///     <para xml:lang="zh">范围流及文件信息</para>
    /// </returns>
    public async Task<(Stream Stream, long TotalLength, long RangeStart, long RangeEnd, GridFSFileInfo FileInfo)> DownloadRangeAsync(ObjectId id, long startByte, long? endByte = null, CancellationToken cancellationToken = default)
    {
        // 获取文件信息
        var fileInfo = await (await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, id), cancellationToken: cancellationToken))
                           .FirstOrDefaultAsync(cancellationToken) ??
                       throw new FileNotFoundException($"File with ID {id} not found");
        var totalLength = fileInfo.Length;
        var actualStart = Math.Max(0, startByte);
        var actualEnd = endByte.HasValue ? Math.Min(endByte.Value, totalLength - 1) : totalLength - 1;
        if (actualStart >= totalLength)
        {
            throw new ArgumentOutOfRangeException(nameof(startByte), "Start byte is beyond file length");
        }
        // 打开可定位的下载流
        var fullStream = await _bucket.OpenDownloadStreamAsync(id, new() { Seekable = true }, cancellationToken);
        // 定位到起始位置
        fullStream.Seek(actualStart, SeekOrigin.Begin);
        // 创建范围限制流
        var rangeLength = (actualEnd - actualStart) + 1;
        var rangeStream = new RangeStream(fullStream, rangeLength);
        return (rangeStream, totalLength, actualStart, actualEnd, fileInfo);
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