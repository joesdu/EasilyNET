using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.SearchIndex;

/// <summary>
/// Search Index 生命周期管理器，负责 Search Index 的创建、更新和状态检查
/// </summary>
internal static class SearchIndexManager
{
    /// <summary>
    /// 获取集合上现有的所有 Search Index
    /// </summary>
    internal static async Task<Dictionary<string, BsonDocument>> GetExistingSearchIndexesAsync(IMongoCollection<BsonDocument> collection, ILogger? logger, CancellationToken ct = default)
    {
        var indexes = new Dictionary<string, BsonDocument>(StringComparer.Ordinal);
        try
        {
            using var cursor = await collection.SearchIndexes.ListAsync(cancellationToken: ct).ConfigureAwait(false);
            while (await cursor.MoveNextAsync(ct).ConfigureAwait(false))
            {
                foreach (var doc in cursor.Current)
                {
                    if (doc.Contains("name"))
                    {
                        indexes[doc["name"].AsString] = doc;
                    }
                }
            }
        }
        catch (MongoCommandException ex) when (ex.CodeName is "CommandNotFound" or "AtlasSearchNotEnabled" ||
                                               ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                                               ex.Message.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Atlas Search is not available on this MongoDB deployment. Search index management will be skipped.");
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to list search indexes for collection {CollectionName}. Search index management will be skipped.",
                    collection.CollectionNamespace.CollectionName);
            }
        }
        return indexes;
    }

    /// <summary>
    /// 创建 Search Index
    /// </summary>
    internal static async Task CreateSearchIndexAsync(IMongoCollection<BsonDocument> collection, string indexName, SearchIndexType indexType, BsonDocument definition, ILogger? logger, CancellationToken ct = default)
    {
        try
        {
            var model = new CreateSearchIndexModel(indexName, indexType, definition);
            await collection.SearchIndexes.CreateOneAsync(model, ct).ConfigureAwait(false);
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Created search index {IndexName} (type={IndexType}) on collection {CollectionName}.",
                    indexName, indexType, collection.CollectionNamespace.CollectionName);
            }
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Search index {IndexName} already exists on collection {CollectionName}. Skipping creation.",
                    indexName, collection.CollectionNamespace.CollectionName);
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to create search index {IndexName} on collection {CollectionName}.",
                    indexName, collection.CollectionNamespace.CollectionName);
            }
        }
    }

    /// <summary>
    /// 更新 Search Index 定义
    /// </summary>
    internal static async Task UpdateSearchIndexAsync(IMongoCollection<BsonDocument> collection, string indexName, BsonDocument definition, ILogger? logger, CancellationToken ct = default)
    {
        try
        {
            await collection.SearchIndexes.UpdateAsync(indexName, definition, ct).ConfigureAwait(false);
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Updated search index {IndexName} on collection {CollectionName}.",
                    indexName, collection.CollectionNamespace.CollectionName);
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to update search index {IndexName} on collection {CollectionName}.",
                    indexName, collection.CollectionNamespace.CollectionName);
            }
        }
    }
}