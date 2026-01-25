using EasilyNET.Mongo.AspNetCore.Abstraction;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS resumable upload helper - supports breakpoint resume for large file uploads</para>
///     <para xml:lang="zh">GridFS 断点续传辅助类 - 支持大文件上传的断点续传</para>
/// </summary>
public sealed class GridFSHelper : IGridFSUploadService
{
    private readonly IGridFSBucket _bucket;
    private readonly ChunkProcessor _chunkProcessor;
    private readonly ILogger<GridFSHelper> _logger;
    private readonly UploadSessionManager _sessionManager;
    private readonly IUploadValidator _uploadValidator;

    /// <summary>
    ///     <para xml:lang="en">Initialize resumable upload helper</para>
    ///     <para xml:lang="zh">初始化断点续传辅助类</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="uploadValidator">
    ///     <para xml:lang="en">Upload validator</para>
    ///     <para xml:lang="zh">上传验证器</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">Logger instance (optional)</para>
    ///     <para xml:lang="zh">日志记录器实例(可选)</para>
    /// </param>
    public GridFSHelper(IGridFSBucket bucket, IUploadValidator uploadValidator, ILogger<GridFSHelper>? logger = null)
    {
        _bucket = bucket;
        _logger = logger ?? NullLogger<GridFSHelper>.Instance;
        _sessionManager = new(_bucket, _logger);
        _chunkProcessor = new(_bucket, _logger);
        _uploadValidator = uploadValidator;
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
        int sessionExpirationHours = GridFSDefaults.DefaultSessionExpirationHours,
        CancellationToken cancellationToken = default)
    {
        await _uploadValidator.ValidateSessionAsync(filename, totalSize, contentType, chunkSize, fileHash, cancellationToken);
        await _uploadValidator.ValidateContentTypeAsync(filename, contentType, cancellationToken);
        return await _sessionManager.CreateSessionAsync(filename, totalSize, fileHash, contentType, chunkSize, sessionExpirationHours, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Get upload session by session ID</para>
    ///     <para xml:lang="zh">通过会话 ID 获取上传会话</para>
    /// </summary>
    public async Task<GridFSUploadSession?> GetSessionAsync(string sessionId, CancellationToken cancellationToken = default) => await _sessionManager.GetSessionAsync(sessionId, cancellationToken);

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
        await _uploadValidator.ValidateChunkAsync(session, chunkNumber, data, chunkHash, cancellationToken);
        if (chunkNumber == 0)
        {
            await _uploadValidator.ValidateContentTypeAsync(session.Filename, session.ContentType, cancellationToken);
            await _uploadValidator.ValidateMagicNumberAsync(session.Filename, data, cancellationToken);
        }
        // 检查块是否已上传 - 从数据库中查询而不是依赖内存中的 session.UploadedChunks
        // 注意: 由于我们现在直接写入 fs.chunks, 这里的 n 是 GridFS 的块索引, 而不是上传分片的索引
        // 一个上传分片可能对应多个 GridFS 块
        var chunksPerUpload = session.ChunkSize / GridFSDefaults.StandardChunkSize;
        var baseN = chunkNumber * chunksPerUpload;
        var fileId = ObjectId.Parse(session.FileId!);
        // 只要检查第一个子块是否存在即可
        if (await _chunkProcessor.ChunkExistsAsync(fileId, baseN, cancellationToken))
        {
            // 块已存在,同步 session.UploadedChunks 并返回
            // ReSharper disable once InvertIf
            if (!session.UploadedChunks.Contains(chunkNumber))
            {
                await _sessionManager.SyncUploadedChunkAsync(sessionId, chunkNumber, cancellationToken);
            }
            return await GetSessionAsync(sessionId, cancellationToken) ?? session;
        }
        // 验证块哈希
        if (!ChunkProcessor.ValidateChunkHash(data, chunkHash))
        {
            throw new InvalidOperationException($"Chunk hash verification failed. Expected: {chunkHash}");
        }
        if (!await _chunkProcessor.WriteChunkAsync(fileId, chunkNumber, session.ChunkSize, data, cancellationToken))
        {
            // 如果发生重复键异常,说明该块已经存在(并发上传)
            // 同步会话状态并返回
            await _sessionManager.SyncUploadedChunkAsync(sessionId, chunkNumber, cancellationToken);
            return await GetSessionAsync(sessionId, cancellationToken) ?? session;
        }

        // 更新会话 - 添加块号到 UploadedChunks 列表
        await _sessionManager.UpdateSessionChunkAsync(sessionId, chunkNumber, data.Length, cancellationToken);
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
            var safeSessionId = sessionId.Replace("\r", string.Empty).Replace("\n", string.Empty);
            _logger.LogDebug("FinalizeUploadAsync started for session: {SessionId}", safeSessionId);
            var session = await GetSessionAsync(sessionId, cancellationToken) ?? throw new InvalidOperationException($"Session {sessionId} not found");
            if (session.Status == UploadStatus.Completed)
            {
                return ObjectId.Parse(session.FileId!);
            }
            await _uploadValidator.ValidateFinalizeAsync(session, verifyHash, skipHashValidation, cancellationToken);
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
            _logger.LogDebug("Expected chunks: {TotalChunks}, TotalSize: {TotalSize}, ChunkSize: {ChunkSize}", totalChunks, session.TotalSize, session.ChunkSize);
            // 从数据库中查询实际上传的块,而不是依赖 session.UploadedChunks
            var fileIdObj = ObjectId.Parse(session.FileId!);
            var actualUploadedChunks = await _chunkProcessor.GetUploadedChunkIndicesAsync(fileIdObj, session.ChunkSize, cancellationToken);
            _logger.LogDebug("Actual uploaded chunks (mapped): {UploadedChunks}", actualUploadedChunks);

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
            _logger.LogDebug("All chunks validated, creating GridFS file entry");

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
                    computedHash = await _chunkProcessor.CalculateFileHashAsync(fileIdObj, cancellationToken);
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
                    _logger.LogInformation("Duplicate file found after upload: {ExistingFileId}. Deleting new file {FileId}...", existingFile.Id, fileId);
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
            await _chunkProcessor.CreateFileDocumentAsync(fileId, session, computedHash, cancellationToken);

            // 更新会话
            await UpdateSessionCompletedAsync(sessionId, fileId, computedHash, cancellationToken);
            _logger.LogDebug("FinalizeUploadAsync completed successfully");
            return fileId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "FinalizeUploadAsync failed: {ExceptionType}", ex.GetType().Name);
            if (ex.InnerException != null)
            {
                _logger.LogError(ex.InnerException, "FinalizeUploadAsync InnerException: {InnerExceptionType}", ex.InnerException.GetType().Name);
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
        return await _chunkProcessor.GetMissingChunksAsync(ObjectId.Parse(session.FileId!), totalChunks, session.ChunkSize, cancellationToken);
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
            await _chunkProcessor.CleanupTempDataAsync(ObjectId.Parse(session.FileId), cancellationToken);
        }
        await _sessionManager.CancelSessionAsync(sessionId, deleteSession, cancellationToken);
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
    ///     <para xml:lang="en">Rename a file in GridFS</para>
    ///     <para xml:lang="zh">重命名 GridFS 中的文件</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">File ID</para>
    ///     <para xml:lang="zh">文件 ID</para>
    /// </param>
    /// <param name="newName">
    ///     <para xml:lang="en">New filename</para>
    ///     <para xml:lang="zh">新文件名</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    public async Task RenameAsync(string id, string newName, CancellationToken cancellationToken = default)
    {
        await _bucket.RenameAsync(ObjectId.Parse(id), newName, cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Delete files by IDs with reference counting</para>
    ///     <para xml:lang="zh">根据 ID 删除文件（带引用计数）</para>
    /// </summary>
    /// <param name="ids">
    ///     <para xml:lang="en">File IDs to delete</para>
    ///     <para xml:lang="zh">要删除的文件 ID 集合</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Deleted filenames</para>
    ///     <para xml:lang="zh">已删除的文件名</para>
    /// </returns>
    public async Task<IEnumerable<string>> DeleteAsync(string[] ids, CancellationToken cancellationToken = default)
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
                _logger.LogInformation("File {FileId} deleted (refCount: {RefCount})", fileId, refCount);
            }
            else
            {
                _logger.LogInformation("File {FileId} refCount decremented to {RefCount}", fileId, refCount);
            }
        }
        else
        {
            _logger.LogWarning("File {FileId} not found during delete", fileId);
        }
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
        await _sessionManager.UpdateSessionCompletedAsync(sessionId, fileId, fileHash, cancellationToken);
    }

    private async Task<ObjectId> TryDeduplicateAsync(string sessionId, ObjectId originalFileId, string fileHash, CancellationToken cancellationToken)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq("metadata.fileHash", fileHash);
        var existingFile = await (await _bucket.FindAsync(filter, cancellationToken: cancellationToken)).FirstOrDefaultAsync(cancellationToken);
        if (existingFile == null)
        {
            return ObjectId.Empty;
        }
        _logger.LogInformation("Duplicate file found (pre-check): {ExistingFileId}", existingFile.Id);
        await IncrementRefCountAsync(existingFile.Id, cancellationToken);
        await UpdateSessionCompletedAsync(sessionId, existingFile.Id, fileHash, cancellationToken);
        await _chunkProcessor.CleanupTempDataAsync(originalFileId, cancellationToken);
        return existingFile.Id;
    }
}