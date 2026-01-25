using System.Security.Cryptography;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">Processes chunk upload, validation, and storage operations for GridFS</para>
///     <para xml:lang="zh">处理 GridFS 的块上传、验证和存储操作</para>
/// </summary>
internal sealed class ChunkProcessor
{
    private readonly IGridFSBucket _bucket;
    private readonly IMongoCollection<BsonDocument> _chunksCollection;
    private readonly ILogger _logger;

    /// <summary>
    ///     <para xml:lang="en">Initialize chunk processor</para>
    ///     <para xml:lang="zh">初始化块处理器</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">Logger instance (optional)</para>
    ///     <para xml:lang="zh">日志记录器实例(可选)</para>
    /// </param>
    public ChunkProcessor(IGridFSBucket bucket, ILogger? logger = null)
    {
        _bucket = bucket;
        _logger = logger ?? NullLogger.Instance;
        _chunksCollection = bucket.Database.GetCollection<BsonDocument>($"{bucket.Options.BucketName}.chunks");
    }

    /// <summary>
    ///     <para xml:lang="en">Check if a chunk already exists in the database</para>
    ///     <para xml:lang="zh">检查块是否已存在于数据库中</para>
    /// </summary>
    public async Task<bool> ChunkExistsAsync(ObjectId fileId, int baseN, CancellationToken cancellationToken = default)
    {
        var chunkExistsFilter = Builders<BsonDocument>.Filter.And(Builders<BsonDocument>.Filter.Eq("files_id", fileId),
            Builders<BsonDocument>.Filter.Eq("n", baseN));
        var existingChunk = await _chunksCollection.Find(chunkExistsFilter).FirstOrDefaultAsync(cancellationToken);
        return existingChunk is not null;
    }

    /// <summary>
    ///     <para xml:lang="en">Validate chunk hash against provided hash</para>
    ///     <para xml:lang="zh">验证块哈希与提供的哈希是否一致</para>
    /// </summary>
    public static bool ValidateChunkHash(byte[] data, string expectedHash)
    {
        var computedHashBytes = SHA256.HashData(data);
        var computedHash = Convert.ToHexString(computedHashBytes);
        return computedHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     <para xml:lang="en">Write chunk data to GridFS chunks collection, splitting into standard 2MB chunks</para>
    ///     <para xml:lang="zh">将块数据写入 GridFS 块集合,拆分为标准 2MB 块</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">True if write succeeded, false if duplicate key (chunk already exists)</para>
    ///     <para xml:lang="zh">写入成功返回 true,重复键(块已存在)返回 false</para>
    /// </returns>
    public async Task<bool> WriteChunkAsync(ObjectId fileId, int chunkNumber, int sessionChunkSize, byte[] data, CancellationToken cancellationToken = default)
    {
        // 将上传的大块拆分为 GridFS 标准块 (2MB)
        var baseChunkIndex = chunkNumber * (sessionChunkSize / GridFSDefaults.StandardChunkSize);
        var subChunks = new List<BsonDocument>();
        for (var i = 0; i < data.Length; i += GridFSDefaults.StandardChunkSize)
        {
            var length = Math.Min(GridFSDefaults.StandardChunkSize, data.Length - i);
            var subChunkData = new byte[length];
            Array.Copy(data, i, subChunkData, 0, length);
            var n = baseChunkIndex + (i / GridFSDefaults.StandardChunkSize);
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
            return true;
        }
        catch (MongoBulkWriteException ex) when (ex.WriteErrors.Any(e => e.Category == ServerErrorCategory.DuplicateKey))
        {
            // 如果发生重复键异常,说明该块已经存在(并发上传)
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get uploaded chunk indices from database</para>
    ///     <para xml:lang="zh">从数据库获取已上传的块索引</para>
    /// </summary>
    public async Task<List<int>> GetUploadedChunkIndicesAsync(ObjectId fileId, int sessionChunkSize, CancellationToken cancellationToken = default)
    {
        var chunkFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileId);
        var uploadedChunkNumbers = await _chunksCollection.Find(chunkFilter)
                                                          .Project(Builders<BsonDocument>.Projection.Include("n"))
                                                          .ToListAsync(cancellationToken);
        var chunksPerUpload = Math.Max(1, sessionChunkSize / GridFSDefaults.StandardChunkSize);
        var actualUploadedChunks = uploadedChunkNumbers
                                   .Select(doc => doc["n"].AsInt32 / chunksPerUpload)
                                   .Distinct()
                                   .OrderBy(n => n)
                                   .ToList();
        return actualUploadedChunks;
    }

    /// <summary>
    ///     <para xml:lang="en">Get missing chunk numbers for a session</para>
    ///     <para xml:lang="zh">获取会话中缺失的块编号</para>
    /// </summary>
    public async Task<List<int>> GetMissingChunksAsync(ObjectId fileId, int totalChunks, int sessionChunkSize, CancellationToken cancellationToken = default)
    {
        var actualUploadedChunks = await GetUploadedChunkIndicesAsync(fileId, sessionChunkSize, cancellationToken);
        var actualUploadedSet = actualUploadedChunks.ToHashSet();
        var allChunks = Enumerable.Range(0, totalChunks).ToList();
        var missingChunks = allChunks.Where(n => !actualUploadedSet.Contains(n)).ToList();
        return missingChunks;
    }

    /// <summary>
    ///     <para xml:lang="en">Calculate file hash from all chunks in order</para>
    ///     <para xml:lang="zh">按顺序从所有块计算文件哈希</para>
    /// </summary>
    public async Task<string> CalculateFileHashAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Calculating hash for verification...");
        using var sha256 = SHA256.Create();
        var sort = Builders<BsonDocument>.Sort.Ascending("n");
        var cursor = await _chunksCollection.Find(Builders<BsonDocument>.Filter.Eq("files_id", fileId))
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
        return Convert.ToHexString(sha256.Hash!);
    }

    /// <summary>
    ///     <para xml:lang="en">Clean up temporary chunk data for a file</para>
    ///     <para xml:lang="zh">清理文件的临时块数据</para>
    /// </summary>
    public async Task CleanupTempDataAsync(ObjectId fileId, CancellationToken cancellationToken = default)
    {
        var deleteFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileId);
        await _chunksCollection.DeleteManyAsync(deleteFilter, cancellationToken);
        _logger.LogDebug("Temporary chunks cleaned up for fileId: {FileId}", fileId);
    }

    /// <summary>
    ///     <para xml:lang="en">Create the GridFS files document to complete the upload</para>
    ///     <para xml:lang="zh">创建 GridFS 文件文档以完成上传</para>
    /// </summary>
    public async Task CreateFileDocumentAsync(ObjectId fileId, GridFSUploadSession session, string fileHash, CancellationToken cancellationToken = default)
    {
        var filesCollection = _bucket.Database.GetCollection<BsonDocument>($"{_bucket.Options.BucketName}.files");
        var fileDoc = new BsonDocument
        {
            { "_id", fileId },
            { "length", session.TotalSize },
            { "chunkSize", GridFSDefaults.StandardChunkSize },
            { "uploadDate", DateTime.UtcNow },
            { "filename", session.Filename },
            { "contentType", session.ContentType is null ? BsonNull.Value : new BsonString(session.ContentType) },
            { "metadata", new BsonDocument() }
        };

        // 添加自定义元数据
        if (!fileDoc["metadata"].AsBsonDocument.Contains("fileHash"))
        {
            fileDoc["metadata"].AsBsonDocument.Add("fileHash", fileHash);
        }
        if (!fileDoc["metadata"].AsBsonDocument.Contains("refCount"))
        {
            fileDoc["metadata"].AsBsonDocument.Add("refCount", 1);
        }
        await filesCollection.InsertOneAsync(fileDoc, cancellationToken: cancellationToken);
    }
}