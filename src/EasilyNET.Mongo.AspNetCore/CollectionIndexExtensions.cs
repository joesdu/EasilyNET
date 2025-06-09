using System.Collections;
using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

// ReSharper disable PropertyCanBeMadeInitOnly.Local

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MongoDB 集合索引扩展类
/// </summary>
public static class CollectionIndexExtensions
{
    private static readonly ConcurrentDictionary<string, byte> CollectionCache = [];
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = [];

    /// <summary>
    /// 对标记 MongoContext 的实体对象，自动创建 MongoDB 索引
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    public static IApplicationBuilder UseCreateMongoIndexes<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var db = app.ApplicationServices.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        var logger = app.ApplicationServices.GetService<ILogger<T>>();

        // 获取MongoOptions配置
        var options = app.ApplicationServices.GetRequiredService<BasicClientOptions>();
        var useCamelCase =
            options is { DefaultConventionRegistry: true, ConventionRegistry.Values.Count: 0 } ||
            options.ConventionRegistry.Values.Any(pack => pack.Conventions.Any(c => c is CamelCaseElementNameConvention));
        foreach (var collectionName in db.Database.ListCollectionNames().ToEnumerable().Where(c => c.IsNotNullOrWhiteSpace()))
        {
            if (collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            CollectionCache.TryAdd(collectionName, 0);
        }
        try
        {
            EnsureIndexes(db, useCamelCase, logger);
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to create MongoDB indexes for context {ContextType}.", typeof(T).Name);
            }
            throw;
        }
        return app;
    }

    private static void EnsureIndexes<T>(T dbContext, bool useCamelCase, ILogger? logger) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        var dbContextType = dbContext.GetType().DeclaringType ?? dbContext.GetType();

        // 缓存反射结果
        if (!PropertyCache.TryGetValue(dbContextType, out var properties))
        {
            properties = AssemblyHelper.FindTypes(t => t == dbContextType)
                                       .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                       .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>))
                                       .ToArray();
            PropertyCache.TryAdd(dbContextType, properties);
        }
        var timeSeriesTypes = AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false).ToHashSet();
        foreach (var prop in properties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            if (timeSeriesTypes.Contains(entityType))
            {
                continue;
            }
            var hasIndexAttribute = entityType.GetProperties().Any(p => p.GetCustomAttributes(typeof(MongoIndexAttribute), false).Length != 0);
            var hasCompoundIndexAttribute = entityType.GetCustomAttributes(typeof(MongoCompoundIndexAttribute), false).Length != 0;
            if (!hasIndexAttribute && !hasCompoundIndexAttribute)
            {
                continue;
            }
            string? collectionName;
            if (prop.GetValue(dbContext) is IMongoCollection<BsonDocument> collection)
            {
                collectionName = collection.CollectionNamespace.CollectionName;
                if (collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (CollectionCache.TryAdd(collectionName, 0))
                {
                    dbContext.Database.CreateCollection(collectionName);
                    if (logger is not null && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Created collection {CollectionName}.", collectionName);
                    }
                }
                EnsureIndexesForCollection(collection, entityType, useCamelCase, logger);
            }
            else if (prop.GetValue(dbContext) is not null)
            {
                var collectionNameProp = prop.PropertyType.GetProperty(nameof(IMongoCollection<>.CollectionNamespace));
                var collectionNamespace = collectionNameProp?.GetValue(prop.GetValue(dbContext));
                var nameProp = collectionNamespace?.GetType().GetProperty(nameof(IMongoCollection<>.CollectionNamespace.CollectionName));
                collectionName = nameProp?.GetValue(collectionNamespace)?.ToString();
                if (string.IsNullOrEmpty(collectionName) || collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (CollectionCache.TryAdd(collectionName, 0))
                {
                    dbContext.Database.CreateCollection(collectionName);
                    if (logger is not null && logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Created collection {CollectionName}.", collectionName);
                    }
                }
                var bsonCollection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
                EnsureIndexesForCollection(bsonCollection, entityType, useCamelCase, logger);
            }
        }
    }

    private static void EnsureIndexesForCollection(IMongoCollection<BsonDocument> collection, Type type, bool useCamelCase, ILogger? logger)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;

        // 检查是否为时序集合并获取时序字段信息
        var isTimeSeries = IsTimeSeriesCollection(collection.Database, collectionName);
        var timeSeriesFields = GetTimeSeriesFields(type);
        if (isTimeSeries && timeSeriesFields.Count > 0)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Detected time-series collection {CollectionName}. Time fields [{TimeFields}] will be excluded from indexing.", collectionName, string.Join(", ", timeSeriesFields));
            }
        }
        try
        {
            // 1. 查询数据库中现有的所有索引
            var existingIndexes = GetExistingIndexes(collection, logger);
            // 2. 生成当前类型需要的所需索引定义
            var requiredIndexes = GenerateRequiredIndexes(type, collectionName, useCamelCase, logger, isTimeSeries, timeSeriesFields);
            // 3. 比对索引并执行相应操作
            ManageIndexes(collection, existingIndexes, requiredIndexes, logger);
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to manage indexes for collection {CollectionName}.", collectionName);
            }
            throw;
        }
    }

    /// <summary>
    /// 获取集合中现有的所有索引
    /// </summary>
    private static Dictionary<string, IndexDefinition> GetExistingIndexes(IMongoCollection<BsonDocument> collection, ILogger? logger)
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
    /// 生成当前类型需要的所有索引定义
    /// </summary>
    private static List<IndexDefinition> GenerateRequiredIndexes(Type type, string collectionName, bool useCamelCase, ILogger? logger, bool isTimeSeries, HashSet<string>? timeSeriesFields = null)
    {
        var requiredIndexes = new List<IndexDefinition>();
        var allIndexFields = new List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)>();
        var allTextFields = new List<string>();
        var allWildcardFields = new List<(string Path, MongoIndexAttribute Attr)>();
        // 收集所有索引字段
        CollectIndexFields(type, useCamelCase, null, allIndexFields, allTextFields, allWildcardFields, timeSeriesFields);
        // 验证文本索引
        ValidateTextIndexes(allIndexFields, allTextFields);
        // 生成单字段索引
        foreach (var (path, attr, declaringType) in allIndexFields.Where(x => x.Attr.Type != EIndexType.Text))
        {
            var indexDef = CreateSingleFieldIndex(path, attr, declaringType, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成通配符索引
        foreach (var (path, attr) in allWildcardFields)
        {
            var indexDef = CreateWildcardIndex(path, attr, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成文本索引
        if (allTextFields.Count > 0)
        {
            var indexDef = CreateTextIndex(allTextFields, allIndexFields, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成复合索引
        var compoundIndexes = type.GetCustomAttributes<MongoCompoundIndexAttribute>(false);
        requiredIndexes.AddRange(compoundIndexes.Select(compoundAttr => CreateCompoundIndex(compoundAttr, type, collectionName, useCamelCase, isTimeSeries)));
        if (logger is not null && logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Generated {Count} required indexes for type {TypeName}.", requiredIndexes.Count, type.Name);
        }
        return requiredIndexes;
    }

    /// <summary>
    /// 验证文本索引
    /// </summary>
    private static void ValidateTextIndexes(List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> allIndexFields, List<string> allTextFields)
    {
        if (allTextFields.Count <= 0)
        {
            return;
        }
        var textIndexFields = allIndexFields.Where(x => x.Attr.Type == EIndexType.Text).ToList();
        if (textIndexFields.Count > 0 && textIndexFields.Any(x => !allTextFields.Contains(x.Path)))
        {
            throw new InvalidOperationException("每个集合只允许一个文本索引，所有文本字段必须包含在同一个文本索引中。");
        }
        // 验证文本索引唯一性约束
        if (textIndexFields.Any(x => x.Attr.Unique))
        {
            throw new InvalidOperationException("文本索引不支持唯一性约束。");
        }
    }

    /// <summary>
    /// 管理索引：比对现有索引和需要的索引，执行增删改操作
    /// </summary>
    private static void ManageIndexes(IMongoCollection<BsonDocument> collection, Dictionary<string, IndexDefinition> existingIndexes, List<IndexDefinition> requiredIndexes, ILogger? logger)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;
        // 1. 检查需要创建或更新的索引
        foreach (var requiredIndex in requiredIndexes)
        {
            // 首先检查是否有相同字段的索引（基于字段匹配，而不是名称匹配）
            var matchingIndex = FindMatchingIndex(existingIndexes.Values, requiredIndex);
            if (matchingIndex is not null)
            {
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
                        if (existingIndexes.ContainsKey(matchingIndex.Name))
                        {
                            try
                            {
                                collection.Indexes.DropOne(matchingIndex.Name);
                            }
                            catch (MongoCommandException ex) when (ex.Message.Contains("index not found"))
                            {
                                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                                {
                                    logger.LogWarning("索引 {IndexName} 在集合 {CollectionName} 中不存在,跳过删除.", matchingIndex.Name, collectionName);
                                }
                            }
                            catch (Exception ex)
                            {
                                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                                {
                                    logger.LogWarning(ex, "Failed to drop unused index {IndexName} from collection {CollectionName}.", matchingIndex.Name, collectionName);
                                }
                            }
                        }
                        else
                        {
                            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                            {
                                logger.LogWarning("Index {IndexName} not found in collection {CollectionName}, skip drop.", matchingIndex.Name, collectionName);
                            }
                        }
                        CreateIndex(collection, requiredIndex, logger);
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
        // 2. 检查需要删除的索引（存在于数据库但不在需要的索引中）
        var requiredIndexNames = requiredIndexes.Select(idx => idx.Name).ToHashSet();
        foreach (var existingIndexName in existingIndexes.Keys.Where(existingIndexName => !requiredIndexNames.Contains(existingIndexName)))
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Dropping unused index {IndexName} from collection {CollectionName}.", existingIndexName, collectionName);
            }
            try
            {
                collection.Indexes.DropOne(existingIndexName);
            }
            catch (Exception ex)
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "Failed to drop unused index {IndexName} from collection {CollectionName}.", existingIndexName, collectionName);
                }
            }
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

    /// <summary>
    /// 创建单字段索引定义
    /// </summary>
    private static IndexDefinition CreateSingleFieldIndex(string path, MongoIndexAttribute attr, Type declaringType, string collectionName, bool isTimeSeries = false)
    {
        var indexName = attr.Name ?? GenerateIndexName(collectionName, path, attr.Type.ToString());
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        // TTL 索引类型验证
        if (attr.ExpireAfterSeconds.HasValue)
        {
            var propertyType = GetNestedPropertyType(declaringType, path.Replace('_', '.'));
            if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
            {
                throw new InvalidOperationException($"TTL 索引字段 '{path}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
            }
        }
        var keys = attr.Type switch
        {
            EIndexType.Ascending   => new(path, 1),
            EIndexType.Descending  => new(path, -1),
            EIndexType.Geo2D       => new(path, "2d"),
            EIndexType.Geo2DSphere => new(path, "2dsphere"),
            EIndexType.Hashed      => new(path, "hashed"),
            EIndexType.Multikey    => new(path, 1),                       // Multikey 自动识别
            EIndexType.Text        => new(path, "text"),                  // Text 索引
            EIndexType.Wildcard    => new BsonDocument(path, "wildcard"), // Wildcard 索引
            _                      => throw new NotSupportedException($"不支持的索引类型 {attr.Type}")
        };
        // 时序集合不支持稀疏索引，需要强制禁用
        var sparse = attr.Sparse;
        if (isTimeSeries && sparse)
        {
            sparse = false; // 时序集合强制禁用稀疏索引
        }
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = keys,
            Unique = attr.Unique,
            Sparse = sparse,
            ExpireAfterSeconds = attr.ExpireAfterSeconds,
            IndexType = attr.Type,
            OriginalPath = path
        };
        // 解析排序规则
        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(attr.Collation))
        {
            try
            {
                var collationDoc = BsonSerializer.Deserialize<BsonDocument>(attr.Collation);
                var locale = collationDoc.GetValue("locale", null)?.AsString;
                if (!string.IsNullOrEmpty(locale))
                {
                    indexDef.Collation = new(locale);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"索引 '{indexName}' 的排序规则 JSON 无效: {attr.Collation}", ex);
            }
        }
        return indexDef;
    }

    /// <summary>
    /// 创建通配符索引定义
    /// </summary>
    private static IndexDefinition CreateWildcardIndex(string path, MongoIndexAttribute attr, string collectionName, bool isTimeSeries = false)
    {
        var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
        if (!wildcardPath.Contains("$**"))
        {
            throw new InvalidOperationException($"通配符索引路径 '{path}' 格式无效，应包含 '$**' 通配符。");
        }
        var indexName = attr.Name ?? GenerateIndexName(collectionName, wildcardPath, "Wildcard");
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        // 时序集合不支持稀疏索引，需要强制禁用
        var sparse = attr.Sparse;
        if (isTimeSeries && sparse)
        {
            sparse = false; // 时序集合强制禁用稀疏索引
        }
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = new(wildcardPath, "$**"),
            Unique = attr.Unique,
            Sparse = sparse,
            IndexType = EIndexType.Wildcard,
            OriginalPath = wildcardPath
        };
        // 解析排序规则
        // ReSharper disable once InvertIf
        if (attr.Collation.IsNotNullOrWhiteSpace())
        {
            try
            {
                var collationDoc = BsonSerializer.Deserialize<BsonDocument>(attr.Collation);
                var locale = collationDoc.GetValue("locale", null)?.AsString;
                if (!string.IsNullOrEmpty(locale))
                {
                    indexDef.Collation = new(locale);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"通配符索引 '{indexName}' 的排序规则 JSON 无效: {attr.Collation}", ex);
            }
        }
        return indexDef;
    }

    /// <summary>
    /// 创建文本索引定义
    /// </summary>
    private static IndexDefinition CreateTextIndex(List<string> textFields, List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> allIndexFields, string collectionName, bool isTimeSeries = false)
    {
        var textIndexName = $"{collectionName}_" + string.Join("_", textFields) + "_Text";
        if (textIndexName.Length > 127)
        {
            textIndexName = TruncateIndexName(textIndexName, 127);
        }
        var keys = new BsonDocument();
        foreach (var field in textFields)
        {
            keys.Add(field, "text");
        }
        var firstTextAttr = allIndexFields.FirstOrDefault(x => x.Attr.Type == EIndexType.Text).Attr;
        // 时序集合不支持稀疏索引，需要强制禁用
        var sparse = firstTextAttr?.Sparse ?? false;
        if (isTimeSeries && sparse)
        {
            sparse = false; // 时序集合强制禁用稀疏索引
        }
        var indexDef = new IndexDefinition
        {
            Name = textIndexName,
            Keys = keys,
            Unique = false, // 文本索引不支持唯一性
            Sparse = sparse,
            IndexType = EIndexType.Text,
            OriginalPath = string.Join(",", textFields)
        };
        // 解析排序规则
        if (!string.IsNullOrWhiteSpace(firstTextAttr?.Collation))
        {
            try
            {
                var collationDoc = BsonSerializer.Deserialize<BsonDocument>(firstTextAttr.Collation);
                var locale = collationDoc.GetValue("locale", null)?.AsString;
                if (!string.IsNullOrEmpty(locale))
                {
                    indexDef.Collation = new(locale);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"文本索引 '{textIndexName}' 的排序规则 JSON 无效: {firstTextAttr.Collation}", ex);
            }
        }
        // 解析文本索引选项
        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(firstTextAttr?.TextIndexOptions))
        {
            try
            {
                var textOptionsDoc = BsonSerializer.Deserialize<BsonDocument>(firstTextAttr.TextIndexOptions);
                if (textOptionsDoc.Contains("weights"))
                {
                    indexDef.Weights = textOptionsDoc["weights"].AsBsonDocument;
                }
                if (textOptionsDoc.Contains("default_language"))
                {
                    indexDef.DefaultLanguage = textOptionsDoc["default_language"].AsString;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"文本索引 '{textIndexName}' 的选项 JSON 无效: {firstTextAttr.TextIndexOptions}", ex);
            }
        }
        return indexDef;
    }

    /// <summary>
    /// 创建复合索引定义
    /// </summary>
    private static IndexDefinition CreateCompoundIndex(MongoCompoundIndexAttribute compoundAttr, Type type, string collectionName, bool useCamelCase, bool isTimeSeries = false)
    {
        var fields = compoundAttr.Fields.Select(f => useCamelCase ? f.ToLowerCamelCase() : f).ToArray();
        var indexName = compoundAttr.Name ?? $"{collectionName}_{string.Join("_", fields)}";
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        // TTL 索引类型验证
        if (compoundAttr.ExpireAfterSeconds.HasValue)
        {
            foreach (var field in compoundAttr.Fields)
            {
                var propertyType = GetNestedPropertyType(type, field);
                if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
                {
                    throw new InvalidOperationException($"复合索引 '{indexName}' 的 TTL 字段 '{field}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
                }
            }
        }
        var keys = new BsonDocument();
        for (var i = 0; i < fields.Length; i++)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            object typeVal = compoundAttr.Types[i] switch
            {
                EIndexType.Ascending   => 1,
                EIndexType.Descending  => -1,
                EIndexType.Geo2D       => "2d",
                EIndexType.Geo2DSphere => "2dsphere",
                EIndexType.Hashed      => "hashed",
                EIndexType.Text        => "text",
                _                      => throw new NotSupportedException($"不支持的索引类型 {compoundAttr.Types[i]}")
            };
            keys.Add(fields[i], BsonValue.Create(typeVal));
        }
        // 时序集合不支持稀疏索引，需要强制禁用
        var sparse = compoundAttr.Sparse;
        if (isTimeSeries && sparse)
        {
            sparse = false; // 时序集合强制禁用稀疏索引
        }
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = keys,
            Unique = compoundAttr.Unique,
            Sparse = sparse,
            ExpireAfterSeconds = compoundAttr.ExpireAfterSeconds,
            IndexType = EIndexType.Ascending, // 复合索引使用默认类型
            OriginalPath = string.Join(",", fields)
        };
        // 解析排序规则
        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(compoundAttr.Collation))
        {
            try
            {
                var collationDoc = BsonSerializer.Deserialize<BsonDocument>(compoundAttr.Collation);
                var locale = collationDoc.GetValue("locale", null)?.AsString;
                if (!string.IsNullOrEmpty(locale))
                {
                    indexDef.Collation = new(locale);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"复合索引 '{indexName}' 的排序规则 JSON 无效: {compoundAttr.Collation}", ex);
            }
        }
        return indexDef;
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
                                      BsonInt32 { Value: 1 }           => builder.Ascending(fieldName),
                                      BsonInt32 { Value: -1 }          => builder.Descending(fieldName),
                                      BsonString { Value: "2d" }       => builder.Geo2D(fieldName),
                                      BsonString { Value: "2dsphere" } => builder.Geo2DSphere(fieldName),
                                      BsonString { Value: "hashed" }   => builder.Hashed(fieldName),
                                      BsonString { Value: "text" }     => builder.Text(fieldName),
                                      _                                => throw new NotSupportedException($"不支持的索引字段类型: {fieldValue}")
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
                BsonInt32 { Value: 1 }           => builder.Ascending(fieldName),
                BsonInt32 { Value: -1 }          => builder.Descending(fieldName),
                BsonString { Value: "2d" }       => builder.Geo2D(fieldName),
                BsonString { Value: "2dsphere" } => builder.Geo2DSphere(fieldName),
                BsonString { Value: "hashed" }   => builder.Hashed(fieldName),
                _                                => throw new NotSupportedException($"不支持的索引类型: {fieldValue}")
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

    private static void CollectIndexFields(
        Type type,
        bool useCamelCase,
        string? parentPath,
        List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> fields,
        List<string> textFields,
        List<(string Path, MongoIndexAttribute Attr)> allWildcardFields,
        HashSet<string>? timeSeriesFields = null)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var propType = prop.PropertyType;
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";

            // 检查是否为时序字段，如果是则跳过
            if (timeSeriesFields != null && timeSeriesFields.Contains(fieldName))
            {
                continue;
            }
            foreach (var attr in prop.GetCustomAttributes<MongoIndexAttribute>(false))
            {
                var path = fullPath.Replace('.', '_');
                switch (attr.Type)
                {
                    case EIndexType.Text:
                        textFields.Add(path);
                        break;
                    case EIndexType.Wildcard:
                    {
                        // 通配符索引：支持 field.$** 格式
                        var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
                        allWildcardFields.Add((wildcardPath, attr));
                        break;
                    }
                    case EIndexType.Ascending:
                    case EIndexType.Descending:
                    case EIndexType.Geo2D:
                    case EIndexType.Geo2DSphere:
                    case EIndexType.Hashed:
                    case EIndexType.Multikey:
                    default:
                    {
                        // 自动检测数组或集合类型并标记为 Multikey
                        if (attr.Type == EIndexType.Multikey ||
                            propType.IsArray ||
                            (propType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string)))
                        {
                            // 为 Multikey 类型创建新的属性实例，保持原有属性设置
                            var multikeyAttr = new MongoIndexAttribute(EIndexType.Multikey)
                            {
                                Name = attr.Name,
                                Unique = attr.Unique,
                                Sparse = attr.Sparse,
                                ExpireAfterSeconds = attr.ExpireAfterSeconds,
                                Collation = attr.Collation,
                                TextIndexOptions = attr.TextIndexOptions
                            };
                            fields.Add((path, multikeyAttr, type));
                        }
                        else
                        {
                            fields.Add((path, attr, type));
                        }
                        break;
                    }
                }
            }

            // 递归处理嵌套对象（排除基础类型、字符串、枚举和集合类型）
            if (propType.IsClass &&
                propType != typeof(string) &&
                !propType.IsEnum &&
                !typeof(IEnumerable).IsAssignableFrom(propType) &&
                !propType.Assembly.GetName().Name!.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                CollectIndexFields(propType, useCamelCase, fullPath, fields, textFields, allWildcardFields, timeSeriesFields);
            }
        }
    }

    /// <summary>
    /// 获取嵌套属性的类型
    /// </summary>
    /// <param name="type">起始类型</param>
    /// <param name="propertyPath">属性路径，以点分隔</param>
    /// <returns>属性类型，如果未找到则返回 null</returns>
    private static Type? GetNestedPropertyType(Type type, string propertyPath)
    {
        if (string.IsNullOrEmpty(propertyPath))
        {
            return type;
        }
        var parts = propertyPath.Split('.');
        var currentType = type;
        foreach (var part in parts)
        {
            var property = currentType.GetProperty(part, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return null;
            }
            currentType = property.PropertyType;

            // 处理可空类型
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                currentType = Nullable.GetUnderlyingType(currentType) ?? currentType;
            }
        }
        return currentType;
    }

    /// <summary>
    /// 生成索引名称
    /// </summary>
    /// <param name="collectionName">集合名称</param>
    /// <param name="fieldPath">字段路径</param>
    /// <param name="indexType">索引类型</param>
    /// <returns>生成的索引名称</returns>
    private static string GenerateIndexName(string collectionName, string fieldPath, string indexType)
    {
        // 清理路径中的特殊字符
        var cleanPath = fieldPath.Replace("$", "").Replace("*", "").Replace(".", "_");
        return $"{collectionName}_{cleanPath}_{indexType}";
    }

    /// <summary>
    /// 截断索引名称以符合MongoDB限制
    /// </summary>
    /// <param name="indexName">原始索引名称</param>
    /// <param name="maxLength">最大长度</param>
    /// <returns>截断后的索引名称</returns>
    private static string TruncateIndexName(string indexName, int maxLength)
    {
        if (indexName.Length <= maxLength)
        {
            return indexName;
        }

        // 保留前缀和后缀，中间用哈希值填充
        var prefix = indexName[..(maxLength / 3)];
        var suffix = indexName[^(maxLength / 3)..];
        var hash = indexName.GetHashCode().ToString("X")[..Math.Min(8, maxLength - prefix.Length - suffix.Length)];
        return $"{prefix}_{hash}_{suffix}";
    }

    /// <summary>
    /// 检测集合是否为时序集合
    /// </summary>
    /// <param name="database">数据库</param>
    /// <param name="collectionName">集合名称</param>
    /// <returns>是否为时序集合</returns>
    private static bool IsTimeSeriesCollection(IMongoDatabase database, string collectionName)
    {
        try
        {
            var collectionInfo = database.ListCollections(new ListCollectionsOptions
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", collectionName)
            }).FirstOrDefault();
            return collectionInfo?.Contains("options") == true && collectionInfo["options"].AsBsonDocument.Contains("timeseries");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取时序集合的时序字段列表
    /// </summary>
    /// <param name="type">实体类型</param>
    /// <returns>时序字段名称集合</returns>
    private static HashSet<string> GetTimeSeriesFields(Type type)
    {
        var timeSeriesFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var timeSeriesAttr = type.GetCustomAttribute<TimeSeriesCollectionAttribute>();
        // ReSharper disable once InvertIf
        if (timeSeriesAttr?.TimeSeriesOptions != null)
        {
            // 添加时间字段
            if (!string.IsNullOrWhiteSpace(timeSeriesAttr.TimeSeriesOptions.TimeField))
            {
                timeSeriesFields.Add(timeSeriesAttr.TimeSeriesOptions.TimeField);
            }

            // 添加元数据字段
            if (!string.IsNullOrWhiteSpace(timeSeriesAttr.TimeSeriesOptions.MetaField))
            {
                timeSeriesFields.Add(timeSeriesAttr.TimeSeriesOptions.MetaField);
            }
        }
        return timeSeriesFields;
    }

    /// <summary>
    /// 索引定义信息
    /// </summary>
    internal class IndexDefinition
    {
        public string Name { get; set; } = string.Empty;

        public BsonDocument Keys { get; set; } = [];

        public bool Unique { get; set; }

        public bool Sparse { get; set; }

        public int? ExpireAfterSeconds { get; set; }

        public Collation? Collation { get; set; }

        public BsonDocument? Weights { get; set; }

        public string? DefaultLanguage { get; set; }

        public EIndexType IndexType { get; set; }

        // ReSharper disable once UnusedAutoPropertyAccessor.Local
        public string OriginalPath { get; set; } = string.Empty;

        /// <summary>
        /// 比较两个索引定义是否相同
        /// </summary>
        public bool Equals(IndexDefinition? other)
        {
            if (other == null)
            {
                return false;
            }
            return Name == other.Name &&
                   Keys.Equals(other.Keys) &&
                   Unique == other.Unique &&
                   Sparse == other.Sparse &&
                   ExpireAfterSeconds == other.ExpireAfterSeconds &&
                   CollationEquals(Collation, other.Collation) &&
                   WeightsEquals(Weights, other.Weights) &&
                   DefaultLanguage == other.DefaultLanguage;
        }

        private static bool CollationEquals(Collation? c1, Collation? c2)
        {
            if (c1 == null && c2 == null)
            {
                return true;
            }
            if (c1 == null || c2 == null)
            {
                return false;
            }
            return c1.Locale == c2.Locale;
        }

        private static bool WeightsEquals(BsonDocument? w1, BsonDocument? w2)
        {
            if (w1 == null && w2 == null)
            {
                return true;
            }
            if (w1 == null || w2 == null)
            {
                return false;
            }
            return w1.Equals(w2);
        }
    }
}