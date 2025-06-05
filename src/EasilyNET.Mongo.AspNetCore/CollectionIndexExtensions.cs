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

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// MongoDB 集合索引扩展类
/// </summary>
public static class CollectionIndexExtensions
{
    private static readonly ConcurrentDictionary<string, byte> CollectionCache = [];
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> IndexCache = [];
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
            logger?.LogError(ex, "Failed to create MongoDB indexes for context {ContextType}.", typeof(T).Name);
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
        var timeSeriesTypes = AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>().ToHashSet();
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
                    logger?.LogInformation("Created collection {CollectionName}.", collectionName);
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
                    logger?.LogInformation("Created collection {CollectionName}.", collectionName);
                }
                var bsonCollection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
                EnsureIndexesForCollection(bsonCollection, entityType, useCamelCase, logger);
            }
        }
    }

    private static void EnsureIndexesForCollection(IMongoCollection<BsonDocument> collection, Type type, bool useCamelCase, ILogger? logger)
    {
        IndexCache.TryAdd(collection.CollectionNamespace.CollectionName, []);
        var existingIndexDocs = collection.Indexes.List().ToList();
        var existingIndexes = existingIndexDocs.Select(idx => idx["name"].AsString).ToHashSet();
        var existingIndexDict = existingIndexDocs.ToDictionary(idx => idx["name"].AsString, idx => idx);
        var allIndexFields = new List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)>();
        var allTextFields = new List<string>();
        var allWildcardFields = new List<(string Path, MongoIndexAttribute Attr)>();
        CollectIndexFields(type, useCamelCase, null, allIndexFields, allTextFields, allWildcardFields); // 验证文本索引
        if (allTextFields.Count > 0)
        {
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
        } // 处理通配符索引
        foreach (var (path, attr) in allWildcardFields)
        {
            // 通配符索引支持多种格式：$**, field.$**, field.subfield.$** 等
            var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
            if (!wildcardPath.Contains("$**"))
            {
                throw new InvalidOperationException($"通配符索引路径 '{path}' 格式无效，应包含 '$**' 通配符。");
            }
            var indexName = attr.Name ?? $"{collection.CollectionNamespace.CollectionName}_{wildcardPath.Replace("$", "").Replace("*", "")}_Wildcard";
            if (indexName.Length > 127)
            {
                throw new InvalidOperationException($"索引名称 '{indexName}' 超过 MongoDB 的 127 字节限制。");
            }
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var expectedKey = new BsonDocument(wildcardPath, new BsonString("$**"));
                if (keys.Equals(expectedKey))
                {
                    needCreate = false;
                }
                else
                {
                    logger?.LogInformation("Dropping outdated wildcard index {IndexName}.", indexName);
                    collection.Indexes.DropOne(indexName);
                }
            }
            if (!needCreate)
            {
                continue;
            }
            var builder = Builders<BsonDocument>.IndexKeys;
            var def = builder.Wildcard(wildcardPath);
            var options = new CreateIndexOptions { Name = indexName, Background = true, Sparse = attr.Sparse, Unique = attr.Unique };
            if (!string.IsNullOrWhiteSpace(attr.Collation))
            {
                try
                {
                    var collationDoc = BsonSerializer.Deserialize<BsonDocument>(attr.Collation);
                    var locale = collationDoc.GetValue("locale", null)?.AsString;
                    if (!string.IsNullOrEmpty(locale))
                    {
                        options.Collation = new(locale);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"索引 '{indexName}' 的排序规则 JSON 无效: {attr.Collation}", ex);
                }
            }
            logger?.LogInformation("Creating wildcard index {IndexName} on {Path}.", indexName, wildcardPath);
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(def, options));
            IndexCache[collection.CollectionNamespace.CollectionName].Add(indexName);
        }

        // 处理文本索引
        if (allTextFields.Count > 0)
        {
            var textIndexName = $"{collection.CollectionNamespace.CollectionName}_" + string.Join("_", allTextFields) + "_Text";
            if (textIndexName.Length > 127)
            {
                throw new InvalidOperationException($"文本索引名称 '{textIndexName}' 超过 MongoDB 的 127 字节限制。");
            }
            var needCreate = true;
            if (existingIndexes.Contains(textIndexName))
            {
                var existing = existingIndexDict[textIndexName];
                var keys = existing["key"].AsBsonDocument;
                var expectedKey = new BsonDocument();
                foreach (var f in allTextFields)
                    expectedKey.Add(f, "text");
                if (keys.Equals(expectedKey))
                {
                    needCreate = false;
                }
                else
                {
                    logger?.LogInformation("Dropping outdated text index {IndexName}.", textIndexName);
                    collection.Indexes.DropOne(textIndexName);
                }
            }
            if (needCreate)
            {
                var builder = Builders<BsonDocument>.IndexKeys;
                var def = builder.Combine(allTextFields.Select(f => builder.Text(f)));
                var options = new CreateIndexOptions { Name = textIndexName, Background = true };
                var firstTextAttr = allIndexFields.FirstOrDefault(x => x.Attr.Type == EIndexType.Text).Attr;
                if (firstTextAttr?.Sparse == true)
                {
                    options.Sparse = true;
                }
                if (!string.IsNullOrWhiteSpace(firstTextAttr?.Collation))
                {
                    try
                    {
                        var collationDoc = BsonSerializer.Deserialize<BsonDocument>(firstTextAttr.Collation);
                        var locale = collationDoc.GetValue("locale", null)?.AsString;
                        if (!string.IsNullOrEmpty(locale))
                        {
                            options.Collation = new(locale);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"文本索引 '{textIndexName}' 的排序规则 JSON 无效: {firstTextAttr.Collation}", ex);
                    }
                }
                if (!string.IsNullOrWhiteSpace(firstTextAttr?.TextIndexOptions))
                {
                    try
                    {
                        var textOptionsDoc = BsonSerializer.Deserialize<BsonDocument>(firstTextAttr.TextIndexOptions);
                        if (textOptionsDoc.Contains("weights"))
                        {
                            options.Weights = textOptionsDoc["weights"].AsBsonDocument;
                        }
                        if (textOptionsDoc.Contains("default_language"))
                        {
                            options.DefaultLanguage = textOptionsDoc["default_language"].AsString;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"文本索引 '{textIndexName}' 的选项 JSON 无效: {firstTextAttr.TextIndexOptions}", ex);
                    }
                }
                logger?.LogInformation("Creating text index {IndexName} on fields {Fields}.", textIndexName, string.Join(", ", allTextFields));
                collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(def, options));
                IndexCache[collection.CollectionNamespace.CollectionName].Add(textIndexName);
            }
        } // 处理单字段索引
        foreach (var (path, attr, declaringType) in allIndexFields.Where(x => x.Attr.Type != EIndexType.Text))
        {
            var indexName = attr.Name ?? $"{collection.CollectionNamespace.CollectionName}_{path}_{attr.Type}";
            if (indexName.Length > 127)
            {
                throw new InvalidOperationException($"索引名称 '{indexName}' 超过 MongoDB 的 127 字节限制。");
            }
            if (attr.ExpireAfterSeconds.HasValue)
            {
                var propertyType = GetNestedPropertyType(declaringType, path.Replace('_', '.'));
                if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
                {
                    throw new InvalidOperationException($"TTL 索引字段 '{path}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
                }
            }
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                var expectedKey = attr.Type switch
                {
                    EIndexType.Ascending   => new(path, 1),
                    EIndexType.Descending  => new(path, -1),
                    EIndexType.Geo2D       => new(path, "2d"),
                    EIndexType.Geo2DSphere => new(path, "2dsphere"),
                    EIndexType.Hashed      => new(path, "hashed"),
                    EIndexType.Multikey    => new BsonDocument(path, 1), // Multikey 自动识别
                    _                      => throw new NotSupportedException($"不支持的索引类型 {attr.Type}")
                };
                if (keys.Equals(expectedKey) && unique == attr.Unique)
                {
                    needCreate = false;
                }
                else
                {
                    logger?.LogInformation("Dropping outdated index {IndexName}.", indexName);
                    collection.Indexes.DropOne(indexName);
                }
            }
            if (!needCreate)
            {
                continue;
            }
            var builder = Builders<BsonDocument>.IndexKeys;
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            var indexDefinition = attr.Type switch
            {
                EIndexType.Ascending   => builder.Ascending(path),
                EIndexType.Descending  => builder.Descending(path),
                EIndexType.Geo2D       => builder.Geo2D(path),
                EIndexType.Geo2DSphere => builder.Geo2DSphere(path),
                EIndexType.Hashed      => builder.Hashed(path),
                EIndexType.Multikey    => builder.Ascending(path), // Multikey 自动识别
                _                      => throw new NotSupportedException($"不支持的索引类型 {attr.Type}")
            };
            var options = new CreateIndexOptions { Name = indexName, Unique = attr.Unique, Background = true, Sparse = attr.Sparse };
            if (attr.ExpireAfterSeconds.HasValue)
            {
                options.ExpireAfter = TimeSpan.FromSeconds(attr.ExpireAfterSeconds.Value);
            }
            if (!string.IsNullOrWhiteSpace(attr.Collation))
            {
                try
                {
                    var collationDoc = BsonSerializer.Deserialize<BsonDocument>(attr.Collation);
                    var locale = collationDoc.GetValue("locale", null)?.AsString;
                    if (!string.IsNullOrEmpty(locale))
                    {
                        options.Collation = new(locale);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"索引 '{indexName}' 的排序规则 JSON 无效: {attr.Collation}", ex);
                }
            }
            logger?.LogInformation("Creating index {IndexName} on {Path} with type {Type}.", indexName, path, attr.Type);
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexDefinition, options));
            IndexCache[collection.CollectionNamespace.CollectionName].Add(indexName);
        }

        // 处理复合索引
        var compoundIndexes = type.GetCustomAttributes<MongoCompoundIndexAttribute>(false);
        foreach (var index in compoundIndexes)
        {
            var fields = index.Fields.Select(f => useCamelCase ? f.ToLowerCamelCase() : f).ToArray();
            var indexName = index.Name ?? $"{collection.CollectionNamespace.CollectionName}_{string.Join("_", fields)}";
            if (indexName.Length > 127)
            {
                throw new InvalidOperationException($"复合索引名称 '{indexName}' 超过 MongoDB 的 127 字节限制。");
            }
            if (index.ExpireAfterSeconds.HasValue)
            {
                foreach (var field in index.Fields)
                {
                    var propertyType = GetNestedPropertyType(type, field);
                    if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
                    {
                        throw new InvalidOperationException($"复合索引 '{indexName}' 的 TTL 字段 '{field}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
                    }
                }
            }
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                var expectedKey = new BsonDocument();
                for (var i = 0; i < fields.Length; i++)
                {
                    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                    object typeVal = index.Types[i] switch
                    {
                        EIndexType.Ascending   => 1,
                        EIndexType.Descending  => -1,
                        EIndexType.Geo2D       => "2d",
                        EIndexType.Geo2DSphere => "2dsphere",
                        EIndexType.Hashed      => "hashed",
                        EIndexType.Text        => "text",
                        _                      => throw new NotSupportedException($"不支持的索引类型 {index.Types[i]}")
                    };
                    expectedKey.Add(fields[i], BsonValue.Create(typeVal));
                }
                if (keys.Equals(expectedKey) && unique == index.Unique)
                {
                    needCreate = false;
                }
                else
                {
                    logger?.LogInformation("Dropping outdated compound index {IndexName}.", indexName);
                    collection.Indexes.DropOne(indexName);
                }
            }
            if (!needCreate)
            {
                continue;
            }
            var builder = Builders<BsonDocument>.IndexKeys;
            var definitions = new IndexKeysDefinition<BsonDocument>[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                definitions[i] = index.Types[i] switch
                {
                    EIndexType.Ascending   => builder.Ascending(fields[i]),
                    EIndexType.Descending  => builder.Descending(fields[i]),
                    EIndexType.Geo2D       => builder.Geo2D(fields[i]),
                    EIndexType.Geo2DSphere => builder.Geo2DSphere(fields[i]),
                    EIndexType.Hashed      => builder.Hashed(fields[i]),
                    EIndexType.Text        => builder.Text(fields[i]),
                    _                      => throw new NotSupportedException($"不支持的索引类型 {index.Types[i]}")
                };
            }
            var combinedDefinition = builder.Combine(definitions);
            var options = new CreateIndexOptions { Name = indexName, Unique = index.Unique, Background = true, Sparse = index.Sparse };
            if (index.ExpireAfterSeconds.HasValue)
            {
                options.ExpireAfter = TimeSpan.FromSeconds(index.ExpireAfterSeconds.Value);
            }
            if (!string.IsNullOrWhiteSpace(index.Collation))
            {
                try
                {
                    var collationDoc = BsonSerializer.Deserialize<BsonDocument>(index.Collation);
                    var locale = collationDoc.GetValue("locale", null)?.AsString;
                    if (!string.IsNullOrEmpty(locale))
                    {
                        options.Collation = new(locale);
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"复合索引 '{indexName}' 的排序规则 JSON 无效: {index.Collation}", ex);
                }
            }
            logger?.LogInformation("Creating compound index {IndexName} on fields {Fields}.", indexName, string.Join(", ", fields));
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(combinedDefinition, options));
            IndexCache[collection.CollectionNamespace.CollectionName].Add(indexName);
        }

        // 清理未使用的索引（排除 _id_ 索引）
        foreach (var index in existingIndexes.Where(index => index != "_id_" && !IndexCache[collection.CollectionNamespace.CollectionName].Contains(index)))
        {
            logger?.LogInformation("Dropping unused index {IndexName} from collection {CollectionName}.", index, collection.CollectionNamespace.CollectionName);
            collection.Indexes.DropOne(index);
        }
    }

    private static void CollectIndexFields(
        Type type,
        bool useCamelCase,
        string? parentPath,
        List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> fields,
        List<string> textFields,
        List<(string Path, MongoIndexAttribute Attr)> allWildcardFields)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            var propType = prop.PropertyType;
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";
            foreach (var attr in prop.GetCustomAttributes<MongoIndexAttribute>(false))
            {
                var path = fullPath.Replace('.', '_');
                if (attr.Type == EIndexType.Text)
                {
                    textFields.Add(path);
                }
                else if (attr.Type == EIndexType.Wildcard)
                {
                    // 通配符索引：支持 field.$** 格式
                    var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
                    allWildcardFields.Add((wildcardPath, attr));
                }
                else
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
                }
            }

            // 递归处理嵌套对象（排除基础类型、字符串、枚举和集合类型）
            if (propType.IsClass &&
                propType != typeof(string) &&
                !propType.IsEnum &&
                !typeof(IEnumerable).IsAssignableFrom(propType) &&
                !propType.Assembly.GetName().Name!.StartsWith("System", StringComparison.OrdinalIgnoreCase))
            {
                CollectIndexFields(propType, useCamelCase, fullPath, fields, textFields, allWildcardFields);
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
}