using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.SearchIndex;

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
    /// 判断生成的索引定义是否已被现有定义满足（子集比较）。
    /// 服务端返回的 latestDefinition 会补充默认值字段，因此只校验我们显式声明的字段：
    /// 声明的每个字段都必须在现有定义中存在且值相等。
    /// 注意：从特性中移除字段不会触发更新（避免误判导致索引反复重建）。
    /// </summary>
    internal static bool IsDefinitionSatisfiedBy(BsonValue expected, BsonValue actual)
    {
        if (expected is BsonDocument expectedDoc && actual is BsonDocument actualDoc)
        {
            return expectedDoc.All(element => actualDoc.TryGetValue(element.Name, out var actualValue) && IsDefinitionSatisfiedBy(element.Value, actualValue));
        }
        if (expected is BsonArray expectedArr && actual is BsonArray actualArr)
        {
            return expectedArr.All(e => actualArr.Any(a => IsDefinitionSatisfiedBy(e, a)));
        }
        // 数值类型宽松比较：服务端可能以 Int64/Double 返回我们写入的 Int32
        if (expected.IsNumeric && actual.IsNumeric)
        {
            return expected.ToDouble().Equals(actual.ToDouble());
        }
        return expected.Equals(actual);
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