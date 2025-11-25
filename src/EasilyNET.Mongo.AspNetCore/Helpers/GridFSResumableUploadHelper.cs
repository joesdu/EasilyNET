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
    private readonly string _tempDir = Path.Combine(AppContext.BaseDirectory, "gridfs_temp");

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
        // 使用独立的集合存储临时块,避免污染标准的 GridFS chunks 集合,并确保索引不冲突
        _chunksCollection = database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.resumable_chunks");
        // 创建索引以提升查询性能
        CreateIndexes();
        // 清理可能存在的脏数据
        CleanupOrphanedGridFSChunks();
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

        // 为块集合创建唯一索引,防止并发上传导致重复块
        var chunkIndexKeys = Builders<BsonDocument>.IndexKeys
                                                   .Ascending("session_id")
                                                   .Ascending("n");
        var chunkIndexModel = new CreateIndexModel<BsonDocument>(chunkIndexKeys,
            new() { Name = "SessionId_ChunkNumber_Index", Unique = true, Background = true });
        _chunksCollection.Indexes.CreateOne(chunkIndexModel);
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
        string? fileHash,
        BsonDocument? metadata = null,
        int? chunkSize = null,
        int sessionExpirationHours = 24,
        CancellationToken cancellationToken = default)
    {
        // 1. 检查是否存在相同哈希的文件 (秒传)
        if (!string.IsNullOrEmpty(fileHash))
        {
            // 统一转换为大写进行比较,确保与存储的格式一致
            fileHash = fileHash.ToUpperInvariant();
            var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
            var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (existingFile != null)
            {
                Console.WriteLine($"[INFO] Instant upload (deduplication) for file: {filename}, hash: {fileHash}");
                // 增加引用计数
                await IncrementRefCountAsync(existingFile.Id, cancellationToken);
                var completedSession = new GridFSUploadSession
                {
                    SessionId = ObjectId.GenerateNewId().ToString(),
                    Filename = filename,
                    TotalSize = totalSize,
                    UploadedSize = totalSize,
                    ChunkSize = chunkSize ?? existingFile.ChunkSizeBytes,
                    Metadata = metadata,
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
        var session = new GridFSUploadSession
        {
            SessionId = ObjectId.GenerateNewId().ToString(),
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
        var chunkExistsFilter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("session_id", sessionId), Builders<BsonDocument>.Filter.Eq("n", chunkNumber));
        var existingChunk = await _chunksCollection.Find(chunkExistsFilter).FirstOrDefaultAsync(cancellationToken);
        if (existingChunk != null)
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
        // 保存块数据到临时文件
        var sessionDir = Path.Combine(_tempDir, sessionId);
        if (!Directory.Exists(sessionDir))
        {
            Directory.CreateDirectory(sessionDir);
        }
        var chunkPath = Path.Combine(sessionDir, chunkNumber.ToString());
        await File.WriteAllBytesAsync(chunkPath, data, cancellationToken);
        // 验证块哈希 (写入后读取校验,确保磁盘数据完整性)
        using (var sha256 = SHA256.Create())
        {
            await using var stream = File.OpenRead(chunkPath);
            var computedHashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
            var computedHash = Convert.ToHexString(computedHashBytes);
            if (!computedHash.Equals(chunkHash, StringComparison.OrdinalIgnoreCase))
            {
                // 校验失败，删除错误文件
                try
                {
                    File.Delete(chunkPath);
                }
                catch
                {
                    /* ignore */
                }
                throw new InvalidOperationException($"Chunk hash verification failed after writing to disk. Expected: {chunkHash}, Got: {computedHash}");
            }
        }
        // 临时存储块元数据(不包含数据本身)
        var chunkDoc = new BsonDocument
        {
            { "session_id", sessionId },
            { "n", chunkNumber },
            // { "data", new BsonBinaryData(data) }, // 不再存储数据到 MongoDB
            { "uploadedAt", DateTime.UtcNow }
        };
        try
        {
            await _chunksCollection.InsertOneAsync(chunkDoc, cancellationToken: cancellationToken);
        }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
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
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">GridFS file ID</para>
    ///     <para xml:lang="zh">GridFS 文件 ID</para>
    /// </returns>
    public async Task<ObjectId> FinalizeUploadAsync(string sessionId, string? verifyHash = null, CancellationToken cancellationToken = default)
    {
        try
        {
            Console.WriteLine($"[DEBUG] FinalizeUploadAsync started for session: {sessionId}");
            var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
            if (session.Status == UploadStatus.Completed)
            {
                return ObjectId.Parse(session.FileId!);
            }

            // 1. 尝试通过 verifyHash 进行去重 (如果提供了 hash)
            if (!string.IsNullOrEmpty(verifyHash))
            {
                // 统一转换为大写
                verifyHash = verifyHash.ToUpperInvariant();
                var existingId = await TryDeduplicateAsync(sessionId, verifyHash, cancellationToken);
                if (existingId != ObjectId.Empty)
                {
                    return existingId;
                }
            }

            // 计算总块数
            var totalChunks = (int)Math.Ceiling((double)session.TotalSize / session.ChunkSize);
            Console.WriteLine($"[DEBUG] Expected chunks: {totalChunks}, TotalSize: {session.TotalSize}, ChunkSize: {session.ChunkSize}");
            // 从数据库中查询实际上传的块,而不是依赖 session.UploadedChunks
            var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
            var uploadedChunkNumbers = await _chunksCollection.Find(chunkFilter)
                                                              .Project(Builders<BsonDocument>.Projection.Include("n"))
                                                              .ToListAsync(cancellationToken);
            var actualUploadedChunks = uploadedChunkNumbers.Select(doc => doc["n"].AsInt32)
                                                           .OrderBy(n => n)
                                                           .ToList();
            Console.WriteLine($"[DEBUG] Actual uploaded chunks: [{string.Join(", ", actualUploadedChunks)}]");

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
            Console.WriteLine("[DEBUG] All chunks validated, starting GridFS upload");
            // 上传到 GridFS
            var uploadOptions = new GridFSUploadOptions
            {
                ChunkSizeBytes = session.ChunkSize,
                Metadata = session.Metadata
            };
            // 开启 GridFS 上传流
            await using var uploadStream = await _bucket.OpenUploadStreamAsync(session.Filename, uploadOptions, cancellationToken);
            // 初始化 SHA256 (总是计算哈希以支持去重)
            using var sha256 = SHA256.Create();
            long totalBytesWritten = 0;
            var sessionDir = Path.Combine(_tempDir, sessionId);

            // 优化: 直接按顺序从文件系统读取,避免 MongoDB 查询和游标开销
            for (var i = 0; i < totalChunks; i++)
            {
                var chunkPath = Path.Combine(sessionDir, i.ToString());
                if (!File.Exists(chunkPath))
                {
                    throw new InvalidOperationException($"Chunk file not found: {chunkPath}");
                }

                // 使用 FileStream 避免一次性加载到内存
                await using var fileStream = new FileStream(chunkPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                    81920, // 80KB buffer for optimal I/O
                    true);
                var chunkData = new byte[fileStream.Length];
                var totalRead = 0;
                while (totalRead < chunkData.Length)
                {
                    var bytesRead = await fileStream.ReadAsync(chunkData.AsMemory(totalRead), cancellationToken);
                    if (bytesRead == 0)
                    {
                        throw new InvalidOperationException($"Unexpected end of file when reading chunk {i}");
                    }
                    totalRead += bytesRead;
                }
                totalBytesWritten += chunkData.Length;
                Console.WriteLine($"[DEBUG] Writing chunk {i}, size: {chunkData.Length} bytes");

                // 更新哈希计算
                sha256.TransformBlock(chunkData, 0, chunkData.Length, null, 0);

                // 写入 GridFS 流
                await uploadStream.WriteAsync(chunkData, cancellationToken);
            }
            Console.WriteLine($"[DEBUG] Total bytes written: {totalBytesWritten}");

            // 验证哈希
            sha256.TransformFinalBlock([], 0, 0);
            var computedHash = Convert.ToHexString(sha256.Hash!);
            Console.WriteLine($"[DEBUG] Computed hash: {computedHash}");
            if (!string.IsNullOrEmpty(verifyHash))
            {
                Console.WriteLine($"[DEBUG] Expected hash: {verifyHash}");
                if (!computedHash.Equals(verifyHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"File hash verification failed. Expected: {verifyHash}, Got: {computedHash}");
                }
            }
            Console.WriteLine("[DEBUG] Closing upload stream");
            // 完成上传
            await uploadStream.CloseAsync(cancellationToken);
            var fileId = uploadStream.Id;
            Console.WriteLine($"[DEBUG] Upload stream closed, fileId: {fileId}");

            // 2. 再次尝试去重 (防止并发或 verifyHash 为空的情况)
            // 此时文件已经上传到 GridFS (fileId), 但 metadata 还没更新
            var dupFilter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", computedHash);
            // 排除刚才上传的文件 (虽然它还没有 fileHash metadata, 但为了保险)
            var existingFile = await (await _bucket.FindAsync(dupFilter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
            if (existingFile != null)
            {
                Console.WriteLine($"[INFO] Duplicate file found after upload: {existingFile.Id}. Deleting new file {fileId}...");
                // 删除刚才上传的冗余文件
                await _bucket.DeleteAsync(fileId, cancellationToken);

                // 增加现有文件的引用计数
                await IncrementRefCountAsync(existingFile.Id, cancellationToken);

                // 更新会话
                await UpdateSessionCompletedAsync(sessionId, existingFile.Id, computedHash, cancellationToken);
                await CleanupTempDataAsync(sessionId, cancellationToken);
                return existingFile.Id;
            }

            // 3. 没有重复, 更新新文件的 metadata (hash + refCount)
            await UpdateFileMetadataAsync(fileId, computedHash, 1, cancellationToken);

            // 更新会话
            await UpdateSessionCompletedAsync(sessionId, fileId, computedHash, cancellationToken);
            await CleanupTempDataAsync(sessionId, cancellationToken);
            Console.WriteLine("[DEBUG] FinalizeUploadAsync completed successfully");
            return fileId;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] FinalizeUploadAsync failed: {ex.GetType().Name}");
            Console.WriteLine($"[ERROR] Message: {ex.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] InnerException: {ex.InnerException.Message}");
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
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
        var uploadedChunkNumbers = await _chunksCollection.Find(chunkFilter).Project(Builders<BsonDocument>.Projection.Include("n")).ToListAsync(cancellationToken);
        var actualUploadedChunks = uploadedChunkNumbers.Select(doc => doc["n"].AsInt32).ToHashSet();
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
        // 清理临时块数据
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
        await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);
        // 清理临时文件
        var sessionDir = Path.Combine(_tempDir, sessionId);
        if (Directory.Exists(sessionDir))
        {
            Directory.Delete(sessionDir, true);
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
    ///     <para xml:lang="en">Clean up expired sessions (can be called periodically by a background job)</para>
    ///     <para xml:lang="zh">清理过期会话(可由后台任务定期调用)</para>
    /// </summary>
    public async Task CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.And(Builders<GridFSUploadSession>.Filter.Lt(s => s.ExpiresAt, DateTime.UtcNow), Builders<GridFSUploadSession>.Filter.Ne(s => s.Status, UploadStatus.Completed));
        var expiredSessions = await _sessionCollection.Find(filter).ToListAsync(cancellationToken);
        foreach (var session in expiredSessions)
        {
            // 清理临时块数据
            var chunkFilter = Builders<BsonDocument>.Filter.Eq("session_id", session.SessionId);
            await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);
            // 清理临时文件
            var sessionDir = Path.Combine(_tempDir, session.SessionId);
            if (Directory.Exists(sessionDir))
            {
                Directory.Delete(sessionDir, true);
            }
            // 标记会话为过期
            var updateFilter = Builders<GridFSUploadSession>.Filter.Eq(s => s.SessionId, session.SessionId);
            var update = Builders<GridFSUploadSession>.Update.Set(s => s.Status, UploadStatus.Expired);
            await _sessionCollection.UpdateOneAsync(updateFilter, update, cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// 清理 GridFS chunks 集合中可能存在的脏数据
    /// </summary>
    private void CleanupOrphanedGridFSChunks()
    {
        try
        {
            var gridfsChunksCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.chunks");
            // 删除 files_id 为 null 的脏数据
            var filter = Builders<BsonDocument>.Filter.Eq("files_id", BsonNull.Value);
            var result = gridfsChunksCollection.DeleteMany(filter);
            if (result.DeletedCount > 0)
            {
                Console.WriteLine($"[INFO] Cleaned up {result.DeletedCount} orphaned GridFS chunks with null files_id");
            }
        }
        catch (Exception ex)
        {
            // 如果清理失败,记录但不中断初始化
            Console.WriteLine($"[WARN] Failed to cleanup orphaned GridFS chunks: {ex.Message}");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Delete file with reference counting</para>
    ///     <para xml:lang="zh">带引用计数的删除文件</para>
    /// </summary>
    public async Task DeleteFileAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);

        // 原子操作: 减少引用计数并返回更新后的文档
        var update = Builders<BsonDocument>.Update.Inc("metadata.refCount", -1);
        var options = new FindOneAndUpdateOptions<BsonDocument> { ReturnDocument = ReturnDocument.After };
        var result = await filesCollection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (result != null)
        {
            int refCount;
            if (result.Contains("metadata") && result["metadata"].IsBsonDocument && result["metadata"].AsBsonDocument.Contains("refCount"))
            {
                refCount = result["metadata"]["refCount"].AsInt32;
            }
            else
            {
                refCount = -1;
            }
            if (refCount <= 0)
            {
                await _bucket.DeleteAsync(fileId, cancellationToken);
                Console.WriteLine($"[INFO] File {fileId} deleted (refCount: {refCount})");
            }
            else
            {
                Console.WriteLine($"[INFO] File {fileId} refCount decremented to {refCount}");
            }
        }
        else
        {
            Console.WriteLine($"[WARN] File {fileId} not found during delete");
        }
    }

    private async Task IncrementRefCountAsync(ObjectId fileId, CancellationToken cancellationToken)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
        var update = Builders<BsonDocument>.Update.Inc("metadata.refCount", 1);
        await filesCollection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private async Task UpdateFileMetadataAsync(ObjectId fileId, string fileHash, int refCount, CancellationToken cancellationToken)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var filter = Builders<BsonDocument>.Filter.Eq("_id", fileId);
        var update = Builders<BsonDocument>.Update
                                           .Set("metadata.fileHash", fileHash)
                                           .Set("metadata.refCount", refCount);
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
        Console.WriteLine("[DEBUG] Session updated");
    }

    private async Task CleanupTempDataAsync(string sessionId, CancellationToken cancellationToken)
    {
        // 清理临时块数据
        var deleteFilter = Builders<BsonDocument>.Filter.Eq("session_id", sessionId);
        await _chunksCollection.DeleteManyAsync(deleteFilter, cancellationToken);

        // 清理临时文件
        var sessionDir = Path.Combine(_tempDir, sessionId);
        if (Directory.Exists(sessionDir))
        {
            Directory.Delete(sessionDir, true);
        }
        Console.WriteLine("[DEBUG] Temporary chunks cleaned up");
    }

    private async Task<ObjectId> TryDeduplicateAsync(string sessionId, string fileHash, CancellationToken cancellationToken)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
        var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
        if (existingFile == null)
        {
            return ObjectId.Empty;
        }
        Console.WriteLine($"[INFO] Duplicate file found (pre-check): {existingFile.Id}");
        await IncrementRefCountAsync(existingFile.Id, cancellationToken);
        await UpdateSessionCompletedAsync(sessionId, existingFile.Id, fileHash, cancellationToken);
        await CleanupTempDataAsync(sessionId, cancellationToken);
        return existingFile.Id;
    }
}