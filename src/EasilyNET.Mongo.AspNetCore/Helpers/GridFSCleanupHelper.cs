using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS file cleanup helper for managing expired or orphaned files</para>
///     <para xml:lang="zh">GridFS 文件清理辅助类,用于管理过期或孤立文件</para>
/// </summary>
public class GridFSCleanupHelper
{
    private readonly IGridFSBucket _bucket;
    private readonly IMongoCollection<BsonDocument> _chunksCollection;
    private readonly IMongoCollection<BsonDocument> _filesCollection;

    /// <summary>
    ///     <para xml:lang="en">Initialize cleanup helper</para>
    ///     <para xml:lang="zh">初始化清理辅助类</para>
    /// </summary>
    /// <param name="bucket">
    ///     <para xml:lang="en">GridFS bucket</para>
    ///     <para xml:lang="zh">GridFS 存储桶</para>
    /// </param>
    public GridFSCleanupHelper(IGridFSBucket bucket)
    {
        _bucket = bucket;
        var bucketName = bucket.Options.BucketName;
        _filesCollection = bucket.Database.GetCollection<BsonDocument>($"{bucketName}.files");
        _chunksCollection = bucket.Database.GetCollection<BsonDocument>($"{bucketName}.chunks");
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Delete files older than specified days. Useful for cleaning up temporary or cached files.
    ///     </para>
    ///     <para xml:lang="zh">删除超过指定天数的文件。适用于清理临时或缓存文件。</para>
    /// </summary>
    /// <param name="days">
    ///     <para xml:lang="en">Number of days (files older than this will be deleted)</para>
    ///     <para xml:lang="zh">天数(超过此天数的文件将被删除)</para>
    /// </param>
    /// <param name="filePattern">
    ///     <para xml:lang="en">File name pattern (regex, optional)</para>
    ///     <para xml:lang="zh">文件名模式(正则表达式,可选)</para>
    /// </param>
    /// <param name="metadataFilter">
    ///     <para xml:lang="en">Additional metadata filter (optional)</para>
    ///     <para xml:lang="zh">额外的元数据过滤条件(可选)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Number of deleted files</para>
    ///     <para xml:lang="zh">删除的文件数</para>
    /// </returns>
    public async Task<long> DeleteOldFilesAsync(
        int days,
        string? filePattern = null,
        FilterDefinition<BsonDocument>? metadataFilter = null,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        var filterBuilder = Builders<BsonDocument>.Filter;

        // 构建过滤条件
        var filters = new List<FilterDefinition<BsonDocument>>
        {
            filterBuilder.Lt("uploadDate", cutoffDate)
        };
        if (!string.IsNullOrEmpty(filePattern))
        {
            filters.Add(filterBuilder.Regex("filename", new(filePattern)));
        }
        if (metadataFilter != null)
        {
            filters.Add(metadataFilter);
        }
        var combinedFilter = filterBuilder.And(filters);

        // 查找符合条件的文件
        var filesToDelete = await _filesCollection.Find(combinedFilter)
                                                  .Project(Builders<BsonDocument>.Projection.Include("_id"))
                                                  .ToListAsync(cancellationToken);
        long deletedCount = 0;

        // 逐个删除文件
        foreach (var file in filesToDelete)
        {
            try
            {
                var fileId = file["_id"].AsObjectId;
                await _bucket.DeleteAsync(fileId, cancellationToken);
                deletedCount++;
            }
            catch (Exception)
            {
                // 记录错误但继续处理其他文件
                // 可以在这里添加日志记录
            }
        }
        return deletedCount;
    }

    /// <summary>
    ///     <para xml:lang="en">Delete files by metadata criteria</para>
    ///     <para xml:lang="zh">根据元数据条件删除文件</para>
    /// </summary>
    /// <param name="metadataKey">
    ///     <para xml:lang="en">Metadata key</para>
    ///     <para xml:lang="zh">元数据键</para>
    /// </param>
    /// <param name="metadataValue">
    ///     <para xml:lang="en">Metadata value</para>
    ///     <para xml:lang="zh">元数据值</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Number of deleted files</para>
    ///     <para xml:lang="zh">删除的文件数</para>
    /// </returns>
    public async Task<long> DeleteByMetadataAsync(
        string metadataKey,
        BsonValue metadataValue,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq($"metadata.{metadataKey}", metadataValue);
        var filesToDelete = await _filesCollection.Find(filter)
                                                  .Project(Builders<BsonDocument>.Projection.Include("_id"))
                                                  .ToListAsync(cancellationToken);
        long deletedCount = 0;
        foreach (var file in filesToDelete)
        {
            try
            {
                var fileId = file["_id"].AsObjectId;
                await _bucket.DeleteAsync(fileId, cancellationToken);
                deletedCount++;
            }
            catch (Exception)
            {
                // 记录错误但继续处理
            }
        }
        return deletedCount;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Clean up orphaned chunks (chunks without corresponding file metadata). This can happen if upload fails.
    ///     </para>
    ///     <para xml:lang="zh">清理孤立的块(没有对应文件元数据的块)。这可能发生在上传失败时。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Number of deleted chunks</para>
    ///     <para xml:lang="zh">删除的块数</para>
    /// </returns>
    public async Task<long> CleanupOrphanedChunksAsync(CancellationToken cancellationToken = default)
    {
        // 获取所有有效的文件 ID
        var validFileIds = await _filesCollection
                                 .Find(Builders<BsonDocument>.Filter.Empty)
                                 .Project(Builders<BsonDocument>.Projection.Include("_id"))
                                 .ToListAsync(cancellationToken);
        var validIdSet = new HashSet<ObjectId>(validFileIds.Select(f => f["_id"].AsObjectId));

        // 查找所有块
        var allChunks = await _chunksCollection
                              .Find(Builders<BsonDocument>.Filter.Empty)
                              .Project(Builders<BsonDocument>.Projection.Include("files_id"))
                              .ToListAsync(cancellationToken);

        // 找出孤立的块
        var orphanedChunkFileIds = allChunks
                                   .Select(c => c["files_id"].AsObjectId)
                                   .Distinct()
                                   .Where(id => !validIdSet.Contains(id))
                                   .ToList();
        long deletedCount = 0;

        // 删除孤立的块
        foreach (var filter in orphanedChunkFileIds.Select(fileId => Builders<BsonDocument>.Filter.Eq("files_id", fileId)))
        {
            var result = await _chunksCollection.DeleteManyAsync(filter, cancellationToken);
            deletedCount += result.DeletedCount;
        }
        return deletedCount;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Get storage statistics including total files, total size, and largest files
    ///     </para>
    ///     <para xml:lang="zh">获取存储统计信息,包括文件总数、总大小和最大的文件</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Storage statistics</para>
    ///     <para xml:lang="zh">存储统计信息</para>
    /// </returns>
    public async Task<GridFSStorageStats> GetStorageStatsAsync(CancellationToken cancellationToken = default)
    {
        var totalFiles = await _filesCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty,
                             cancellationToken: cancellationToken);
        var pipeline = new[]
        {
            new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "totalSize", new BsonDocument("$sum", "$length") }
            })
        };
        var aggregation = await _filesCollection.AggregateAsync<BsonDocument>(pipeline, cancellationToken: cancellationToken);
        var result = await aggregation.FirstOrDefaultAsync(cancellationToken);
        var totalSize = result?["totalSize"].ToInt64() ?? 0;
        var largestFiles = await _filesCollection
                                 .Find(Builders<BsonDocument>.Filter.Empty)
                                 .Sort(Builders<BsonDocument>.Sort.Descending("length"))
                                 .Limit(10)
                                 .ToListAsync(cancellationToken);
        return new()
        {
            TotalFiles = totalFiles,
            TotalSize = totalSize,
            LargestFiles = largestFiles.Select(f => new FileInfo
            {
                Id = f["_id"].AsObjectId.ToString(),
                Filename = f["filename"].AsString,
                Size = f["length"].ToInt64(),
                UploadDate = f["uploadDate"].ToUniversalTime()
            }).ToList()
        };
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create TTL index on files collection to automatically delete files after specified time
    ///     </para>
    ///     <para xml:lang="zh">在文件集合上创建 TTL 索引,以在指定时间后自动删除文件</para>
    /// </summary>
    /// <param name="expireAfterSeconds">
    ///     <para xml:lang="en">Expire after seconds (e.g., 86400 for 24 hours)</para>
    ///     <para xml:lang="zh">过期时间(秒),例如 86400 表示 24 小时</para>
    /// </param>
    /// <param name="metadataField">
    ///     <para xml:lang="en">
    ///     Metadata field to use for TTL (optional, uses uploadDate if not specified)
    ///     </para>
    ///     <para xml:lang="zh">用于 TTL 的元数据字段(可选,未指定时使用 uploadDate)</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    public async Task CreateTTLIndexAsync(
        int expireAfterSeconds,
        string? metadataField = null,
        CancellationToken cancellationToken = default)
    {
        var field = string.IsNullOrEmpty(metadataField) ? "uploadDate" : $"metadata.{metadataField}";
        var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending(field);
        var indexOptions = new CreateIndexOptions
        {
            ExpireAfter = TimeSpan.FromSeconds(expireAfterSeconds),
            Name = $"TTL_{field.Replace(".", "_")}_Index",
            Background = true
        };
        var indexModel = new CreateIndexModel<BsonDocument>(indexKeys, indexOptions);
        await _filesCollection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}

/// <summary>
///     <para xml:lang="en">GridFS storage statistics</para>
///     <para xml:lang="zh">GridFS 存储统计信息</para>
/// </summary>
public class GridFSStorageStats
{
    /// <summary>
    ///     <para xml:lang="en">Total number of files</para>
    ///     <para xml:lang="zh">文件总数</para>
    /// </summary>
    public long TotalFiles { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Total size in bytes</para>
    ///     <para xml:lang="zh">总大小(字节)</para>
    /// </summary>
    public long TotalSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">List of largest files</para>
    ///     <para xml:lang="zh">最大文件列表</para>
    /// </summary>
    public List<FileInfo> LargestFiles { get; set; } = [];
}

/// <summary>
///     <para xml:lang="en">File information</para>
///     <para xml:lang="zh">文件信息</para>
/// </summary>
public class FileInfo
{
    /// <summary>
    ///     <para xml:lang="en">File ID</para>
    ///     <para xml:lang="zh">文件 ID</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Filename</para>
    ///     <para xml:lang="zh">文件名</para>
    /// </summary>
    public string Filename { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">File size in bytes</para>
    ///     <para xml:lang="zh">文件大小(字节)</para>
    /// </summary>
    public long Size { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Upload date</para>
    ///     <para xml:lang="zh">上传日期</para>
    /// </summary>
    public DateTime UploadDate { get; set; }
}