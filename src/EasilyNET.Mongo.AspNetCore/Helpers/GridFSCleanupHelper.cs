using EasilyNET.Mongo.AspNetCore.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using GridFileInfo = EasilyNET.Mongo.AspNetCore.Models.GridFileInfo;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore.Helpers;

/// <summary>
///     <para xml:lang="en">GridFS file cleanup helper for managing expired or orphaned files</para>
///     <para xml:lang="zh">GridFS 文件清理辅助类,用于管理过期或孤立文件</para>
/// </summary>
internal sealed class GridFSCleanupHelper
{
    private readonly IGridFSBucket _bucket;
    private readonly IMongoCollection<BsonDocument> _chunksCollection;
    private readonly IMongoCollection<BsonDocument> _filesCollection;
    private readonly IMongoCollection<GridFSUploadSession> _sessionCollection;

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
        _sessionCollection = bucket.Database.GetCollection<GridFSUploadSession>("fs.upload_sessions");
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
    public async Task<long> DeleteOldFilesAsync(int days, string? filePattern = null, FilterDefinition<BsonDocument>? metadataFilter = null, CancellationToken cancellationToken = default)
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
        var filesToDelete = await _filesCollection.Find(combinedFilter).Project(Builders<BsonDocument>.Projection.Include("_id")).ToListAsync(cancellationToken);
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
    public async Task<long> DeleteByMetadataAsync(string metadataKey, BsonValue metadataValue, CancellationToken cancellationToken = default)
    {
        var filter = Builders<BsonDocument>.Filter.Eq($"metadata.{metadataKey}", metadataValue);
        var filesToDelete = await _filesCollection.Find(filter).Project(Builders<BsonDocument>.Projection.Include("_id")).ToListAsync(cancellationToken);
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
        // 1. 获取所有已完成文件的 ID
        var completedFileIds = await _filesCollection.Distinct<ObjectId>("_id", Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken);
        var validIdSet = new HashSet<ObjectId>(completedFileIds);
        // 2. 获取所有活跃/未过期会话的 FileId
        // 注意: 即使是过期的会话, 如果还没被 CleanupExpiredSessionsAsync 清理, 我们也不应该在这里删除它的块
        // 应该让 CleanupExpiredSessionsAsync 负责清理过期会话的块
        var sessionFileIds = await _sessionCollection.Distinct<string>("FileId", Builders<GridFSUploadSession>.Filter.Empty).ToListAsync(cancellationToken);
        foreach (var id in sessionFileIds
            .Select(idStr => { return ObjectId.TryParse(idStr, out var id) ? id : (ObjectId?)null; })
            .Where(id => id.HasValue)
            .Select(id => id.Value))
        {
            validIdSet.Add(id);
        }
        // 3. 获取所有块的 files_id (去重)
        // 使用 Distinct 优化性能
        var chunkFileIds = await _chunksCollection.Distinct<ObjectId>("files_id", Builders<BsonDocument>.Filter.Empty).ToListAsync(cancellationToken);
        // 4. 找出孤立的 files_id (既不在 fs.files 也不在 fs.upload_sessions)
        var orphanedFileIds = chunkFileIds.Where(id => !validIdSet.Contains(id)).ToList();
        long deletedCount = 0;
        // 5. 删除孤立的块
        foreach (var fileId in orphanedFileIds)
        {
            try
            {
                var filter = Builders<BsonDocument>.Filter.Eq("files_id", fileId);
                var result = await _chunksCollection.DeleteManyAsync(filter, cancellationToken);
                deletedCount += result.DeletedCount;
            }
            catch
            {
                // ignore
            }
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
        var totalFiles = await _filesCollection.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty, cancellationToken: cancellationToken);
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
        var largestFiles = await _filesCollection.Find(Builders<BsonDocument>.Filter.Empty)
                                                 .Sort(Builders<BsonDocument>.Sort.Descending("length"))
                                                 .Limit(10)
                                                 .ToListAsync(cancellationToken);
        return new()
        {
            TotalFiles = totalFiles,
            TotalSize = totalSize,
            LargestFiles = largestFiles.Select(f => new GridFileInfo
            {
                Id = f["_id"].AsObjectId.ToString(),
                Filename = f["filename"].AsString,
                Size = f["length"].ToInt64(),
                UploadDate = f["uploadDate"].ToUniversalTime()
            }).ToList()
        };
    }

    /// <summary>
    ///     <para xml:lang="en">Clean up expired sessions and their temporary files</para>
    ///     <para xml:lang="zh">清理过期会话及其临时文件</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Number of deleted sessions</para>
    ///     <para xml:lang="zh">删除的会话数</para>
    /// </returns>
    public async Task<long> CleanupExpiredSessionsAsync(CancellationToken cancellationToken = default)
    {
        var filter = Builders<GridFSUploadSession>.Filter.Lt(s => s.ExpiresAt, DateTime.UtcNow);
        var expiredSessions = await _sessionCollection.Find(filter).ToListAsync(cancellationToken);
        long deletedCount = 0;
        foreach (var session in expiredSessions)
        {
            try
            {
                // Delete session record
                await _sessionCollection.DeleteOneAsync(s => s.SessionId == session.SessionId, cancellationToken);

                // Delete temp chunks from MongoDB (fs.chunks)
                if (!string.IsNullOrEmpty(session.FileId) && ObjectId.TryParse(session.FileId, out var fileId))
                {
                    var chunkFilter = Builders<BsonDocument>.Filter.Eq("files_id", fileId);
                    await _chunksCollection.DeleteManyAsync(chunkFilter, cancellationToken);
                }
                deletedCount++;
            }
            catch
            {
                // Log error but continue
            }
        }
        return deletedCount;
    }
}