using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Indexing;

/// <summary>
/// 索引生命周期管理器，负责索引的比对、创建、更新和删除
/// </summary>
internal static class IndexManager
{
    /// <summary>
    /// 获取集合中现有的所有索引
    /// </summary>
    internal static Dictionary<string, IndexDefinition> GetExistingIndexes(IMongoCollection<BsonDocument> collection, ILogger? logger)
    {
        var existingIndexes = new Dictionary<string, IndexDefinition>();
        try
        {
            var indexDocs = collection.Indexes.List().ToList();
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Found {Count} existing indexes in collection {CollectionName}.", indexDocs.Count, collection.CollectionNamespace.CollectionName);
            }
            foreach (var indexDoc in indexDocs)
            {
                var indexName = indexDoc["name"].AsString;

                // 跳过默认的 _id 索引
                if (indexName == "_id_")
                {
                    continue;
                }
                var indexDef = new IndexDefinition
                {
                    Name = indexName,
                    Keys = indexDoc["key"].AsBsonDocument,
                    Unique = indexDoc.Contains("unique") && indexDoc["unique"].AsBoolean,
                    Sparse = indexDoc.Contains("sparse") && indexDoc["sparse"].AsBoolean,
                    ExpireAfterSeconds = indexDoc.Contains("expireAfterSeconds") ? indexDoc["expireAfterSeconds"].AsInt32 : null
                };

                // 解析排序规则
                if (indexDoc.Contains("collation"))
                {
                    var collationDoc = indexDoc["collation"].AsBsonDocument;
                    if (collationDoc.Contains("locale"))
                    {
                        indexDef.Collation = new(collationDoc["locale"].AsString);
                    }
                }

                // 解析文本索引权重
                if (indexDoc.Contains("weights"))
                {
                    indexDef.Weights = indexDoc["weights"].AsBsonDocument;
                }

                // 解析默认语言
                if (indexDoc.Contains("default_language"))
                {
                    indexDef.DefaultLanguage = indexDoc["default_language"].AsString;
                }
                existingIndexes[indexName] = indexDef;
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to retrieve existing indexes for collection {CollectionName}.", collection.CollectionNamespace.CollectionName);
            }
            throw;
        }
        return existingIndexes;
    }

    /// <summary>
    /// 管理索引：比对现有索引和需要的索引，执行增删改操作
    /// </summary>
    internal static void ManageIndexes(IMongoCollection<BsonDocument> collection, Dictionary<string, IndexDefinition> existingIndexes, List<IndexDefinition> requiredIndexes, ILogger? logger, BasicClientOptions options)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;
        var processedExistingIndexes = new HashSet<string>();

        // 1. 检查需要创建或更新的索引
        foreach (var requiredIndex in requiredIndexes)
        {
            // 首先检查是否有相同字段的索引（基于字段匹配，而不是名称匹配）
            var matchingIndex = FindMatchingIndex(existingIndexes.Values, requiredIndex);
            if (matchingIndex is not null)
            {
                processedExistingIndexes.Add(matchingIndex.Name);
                // 找到相同字段的索引，比较定义
                if (matchingIndex.Equals(requiredIndex))
                {
                    if (logger is not null && logger.IsEnabled(LogLevel.Debug))
                    {
                        // 定义相同，跳过
                        logger.LogDebug("Index with fields {Fields} already exists with matching definition (name: {IndexName}).", string.Join(", ", requiredIndex.Keys.Names), matchingIndex.Name);
                    }
                }
                else
                {
                    if (logger is not null && logger.IsEnabled(LogLevel.Information))
                    {
                        // 定义不同，需要更新（删除后重建）
                        logger.LogInformation("Updating index {IndexName} in collection {CollectionName} (definition changed).", matchingIndex.Name, collectionName);
                    }
                    try
                    {
                        // 尝试先创建目标索引，以尽量降低“无索引窗口”。
                        // 若 MongoDB 报冲突（常见于同 key 但不同选项），再回退到 Drop + Create。
                        var createdFirst = TryCreateIndexWithFallback(collection, requiredIndex, logger);
                        if (!createdFirst)
                        {
                            DropIndex(collection, matchingIndex.Name, logger);
                            CreateIndex(collection, requiredIndex, logger);
                        }
                        else
                        {
                            DropIndex(collection, matchingIndex.Name, logger);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger is not null && logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(ex, "Failed to update index {IndexName} in collection {CollectionName}.", matchingIndex.Name, collectionName);
                        }
                        throw;
                    }
                }
            }
            else
            {
                // 检查是否存在同名但字段不同的索引（名称冲突）
                if (existingIndexes.ContainsKey(requiredIndex.Name))
                {
                    processedExistingIndexes.Add(requiredIndex.Name);
                    if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Index name collision detected for {IndexName} in collection {CollectionName}. Dropping existing index with different keys.", requiredIndex.Name, collectionName);
                    }
                    try
                    {
                        DropIndex(collection, requiredIndex.Name, logger);
                    }
                    catch (Exception ex)
                    {
                        if (logger is not null && logger.IsEnabled(LogLevel.Error))
                        {
                            logger.LogError(ex, "Failed to drop colliding index {IndexName} in collection {CollectionName}.", requiredIndex.Name, collectionName);
                        }
                        throw;
                    }
                }
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    // 索引不存在，需要创建
                    logger.LogInformation("Creating new index {IndexName} in collection {CollectionName}.", requiredIndex.Name, collectionName);
                }
                try
                {
                    CreateIndex(collection, requiredIndex, logger);
                }
                catch (Exception ex)
                {
                    if (logger is not null && logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Failed to create index {IndexName} in collection {CollectionName}.", requiredIndex.Name, collectionName);
                    }
                    throw;
                }
            }
        }

        // 2. 检查需要删除的索引（存在于数据库但不在需要的索引中且未被处理过）
        foreach (var existingIndexName in existingIndexes.Keys.Where(existingIndexName => !processedExistingIndexes.Contains(existingIndexName)))
        {
            if (!options.DropUnmanagedIndexes)
            {
                // 安全模式：仅记录未管理的索引，不删除
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Unmanaged index detected: {IndexName} in collection {CollectionName}. Set DropUnmanagedIndexes=true to remove.", existingIndexName, collectionName);
                }
                continue;
            }
            // 检查是否为受保护的索引前缀
            if (options.ProtectedIndexPrefixes.Count > 0 && options.ProtectedIndexPrefixes.Any(prefix => existingIndexName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Skipping protected index {IndexName} in collection {CollectionName}.", existingIndexName, collectionName);
                }
                continue;
            }
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Dropping unmanaged index {IndexName} from collection {CollectionName}.", existingIndexName, collectionName);
            }
            DropIndex(collection, existingIndexName, logger);
        }
    }

    /// <summary>
    /// 创建索引
    /// </summary>
    private static void CreateIndex(IMongoCollection<BsonDocument> collection, IndexDefinition indexDef, ILogger? logger)
    {
        var builder = Builders<BsonDocument>.IndexKeys;
        IndexKeysDefinition<BsonDocument> keysDef;

        // 根据索引类型创建不同的索引定义
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (indexDef.IndexType == EIndexType.Wildcard)
        {
            var wildcardField = indexDef.Keys.Names.First();
            keysDef = builder.Wildcard(wildcardField);
        }
        else if (indexDef.IndexType == EIndexType.Text)
        {
            var textFields = indexDef.Keys.Names.Where(name => indexDef.Keys[name].AsString == "text");
            keysDef = builder.Combine(textFields.Select(f => builder.Text(f)));
        }
        else if (indexDef.Keys.ElementCount > 1)
        {
            // 复合索引
            var keyDefinitions = (from element in indexDef.Keys
                                  let fieldName = element.Name
                                  let fieldValue = element.Value
                                  select fieldValue switch
                                  {
                                      BsonInt32 { Value: 1 } => builder.Ascending(fieldName),
                                      BsonInt32 { Value: -1 } => builder.Descending(fieldName),
                                      BsonString { Value: "2d" } => builder.Geo2D(fieldName),
                                      BsonString { Value: "2dsphere" } => builder.Geo2DSphere(fieldName),
                                      BsonString { Value: "hashed" } => builder.Hashed(fieldName),
                                      BsonString { Value: "text" } => builder.Text(fieldName),
                                      _ => throw new NotSupportedException($"不支持的索引字段类型: {fieldValue}")
                                  }).ToList();
            keysDef = builder.Combine(keyDefinitions);
        }
        else
        {
            // 单字段索引
            var fieldName = indexDef.Keys.Names.First();
            var fieldValue = indexDef.Keys[fieldName];
            keysDef = fieldValue switch
            {
                BsonInt32 { Value: 1 } => builder.Ascending(fieldName),
                BsonInt32 { Value: -1 } => builder.Descending(fieldName),
                BsonString { Value: "2d" } => builder.Geo2D(fieldName),
                BsonString { Value: "2dsphere" } => builder.Geo2DSphere(fieldName),
                BsonString { Value: "hashed" } => builder.Hashed(fieldName),
                _ => throw new NotSupportedException($"不支持的索引类型: {fieldValue}")
            };
        }
        var options = new CreateIndexOptions
        {
            Name = indexDef.Name,
            Unique = indexDef.Unique,
            Background = true
        };
        // 只在非时序集合时设置 Sparse
        if (indexDef.Sparse)
        {
            options.Sparse = true;
        }
        if (indexDef.ExpireAfterSeconds.HasValue)
        {
            options.ExpireAfter = TimeSpan.FromSeconds(indexDef.ExpireAfterSeconds.Value);
        }
        if (indexDef.Collation != null)
        {
            options.Collation = indexDef.Collation;
        }
        if (indexDef.Weights != null)
        {
            options.Weights = indexDef.Weights;
        }
        if (!string.IsNullOrEmpty(indexDef.DefaultLanguage))
        {
            options.DefaultLanguage = indexDef.DefaultLanguage;
        }
        try
        {
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(keysDef, options));
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Successfully created index {IndexName} on collection {CollectionName}.", indexDef.Name, collection.CollectionNamespace.CollectionName);
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to create index {IndexName} on collection {CollectionName}.", indexDef.Name, collection.CollectionNamespace.CollectionName);
            }
            throw;
        }
    }

    private static void DropIndex(IMongoCollection<BsonDocument> collection, string indexName, ILogger? logger)
    {
        try
        {
            collection.Indexes.DropOne(indexName);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "IndexNotFound" || ex.Message.Contains("index not found"))
        {
            // Ignore
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to drop index {IndexName} from collection {CollectionName}.", indexName, collection.CollectionNamespace.CollectionName);
            }
            throw;
        }
    }

    private static bool TryCreateIndexWithFallback(IMongoCollection<BsonDocument> collection, IndexDefinition requiredIndex, ILogger? logger)
    {
        try
        {
            CreateIndex(collection, requiredIndex, logger);
            return true;
        }
        catch (MongoCommandException ex) when (ex.CodeName is "IndexOptionsConflict" or "IndexKeySpecsConflict" ||
                                               ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase) ||
                                               ex.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex,
                    "Cannot create replacement index {IndexName} before drop on collection {CollectionName}. Falling back to Drop+Create, which introduces a short no-index window.",
                    requiredIndex.Name, collection.CollectionNamespace.CollectionName);
            }
            return false;
        }
    }

    /// <summary>
    /// 基于字段匹配找到相同的索引（而不是基于名称匹配）
    /// </summary>
    private static IndexDefinition? FindMatchingIndex(IEnumerable<IndexDefinition> existingIndexes, IndexDefinition requiredIndex)
    {
        return existingIndexes.FirstOrDefault(existingIndex => IndexKeysEqual(existingIndex.Keys, requiredIndex.Keys));
    }

    /// <summary>
    /// 比较两个索引键是否相等
    /// </summary>
    private static bool IndexKeysEqual(BsonDocument keys1, BsonDocument keys2)
    {
        return keys1.ElementCount == keys2.ElementCount && keys1.All(element1 => keys2.Contains(element1.Name) && keys2[element1.Name].Equals(element1.Value));
    }
}