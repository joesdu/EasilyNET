using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.AspNetCore.Builder;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">MongoDB collection index extension class</para>
///     <para xml:lang="zh">MongoDB 集合索引扩展类</para>
/// </summary>
public static class CollectionIndexExtensions
{
    private static readonly ConcurrentDictionary<string, byte> CollectionCache = [];
    private static readonly ConcurrentDictionary<string, ConcurrentBag<string>> IndexCache = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB indexes for entity objects marked with
    ///     <see cref="MongoContext" /> and index attributes.
    ///     </para>
    ///     <para xml:lang="zh">对标记 <see cref="MongoContext" /> 的实体对象，自动创建 MongoDB 索引</para>
    /// </summary>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    public static IApplicationBuilder UseCreateMongoIndexes<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var db = app.ApplicationServices.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        // 获取MongoOptions配置
        var options = app.ApplicationServices.GetRequiredService<BasicClientOptions>();
        // 判断是否启用小驼峰
        var useCamelCase =
            // 1. 用户未禁用默认配置，并且默认配置中包含CamelCase
            options is { DefaultConventionRegistry: true, ConventionRegistry.Values.Count: 0 } // 没有自定义覆盖，默认注册
            ||
            // 2. 用户自定义的ConventionRegistry中有CamelCase
            options.ConventionRegistry.Values.Any(pack => pack.Conventions.Any(c => c is CamelCaseElementNameConvention));
        foreach (var collectionName in db.Database.ListCollectionNames().ToEnumerable().Where(c => c.IsNotNullOrWhiteSpace()))
        {
            CollectionCache.TryAdd(collectionName, 0);
        }
        EnsureIndexes(db, useCamelCase);
        return app;
    }

    private static void EnsureIndexes<T>(T dbContext, bool useCamelCase) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        // 获取所有DbContext相关类型
        var dbContextType = dbContext.GetType().DeclaringType ?? dbContext.GetType();
        // 获取所有IMongoCollection<>属性
        var properties = AssemblyHelper.FindTypes(t => t == dbContextType).SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                       .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>)).ToList();
        // 获取所有时序集合类型
        var timeSeriesTypes = AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>().ToHashSet();
        foreach (var prop in properties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            // 跳过时序集合
            if (timeSeriesTypes.Contains(entityType))
            {
                continue;
            }
            // 新增: 跳过未标记索引特性的类型
            var hasIndexAttribute = entityType.GetProperties().Any(p => p.GetCustomAttributes(typeof(MongoIndexAttribute), false).Length is not 0);
            var hasCompoundIndexAttribute = entityType.GetCustomAttributes(typeof(MongoCompoundIndexAttribute), false).Length is not 0;
            if (!hasIndexAttribute && !hasCompoundIndexAttribute)
            {
                continue;
            }
            // 获取IMongoCollection实例
            var collectionObj = prop.GetValue(dbContext);
            string? collectionName;
            if (collectionObj is IMongoCollection<BsonDocument> collection)
            {
                collectionName = collection.CollectionNamespace.CollectionName;
                if (string.Equals(collectionName, "system.profile", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (CollectionCache.TryAdd(collectionName, 0))
                {
                    dbContext.Database.CreateCollection(collectionName);
                }
                EnsureIndexesForCollection(collection, entityType, useCamelCase);
            }
            else if (collectionObj is not null)
            {
                // 兼容IMongoCollection<T>不是BsonDocument的情况
                var collectionNameProp = collectionObj.GetType().GetProperty(nameof(IMongoCollection<>.CollectionNamespace));
                var collectionNamespace = collectionNameProp?.GetValue(collectionObj);
                var nameProp = collectionNamespace?.GetType().GetProperty(nameof(IMongoCollection<>.CollectionNamespace.CollectionName));
                collectionName = nameProp?.GetValue(collectionNamespace)?.ToString();
                if (string.IsNullOrEmpty(collectionName) || string.Equals(collectionName, "system.profile", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (CollectionCache.TryAdd(collectionName, 0))
                {
                    dbContext.Database.CreateCollection(collectionName);
                }
                // 获取BsonDocument集合
                var bsonCollection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
                EnsureIndexesForCollection(bsonCollection, entityType, useCamelCase);
            }
        }
    }

    private static void EnsureIndexesForCollection(IMongoCollection<BsonDocument> collection, Type type, bool useCamelCase)
    {
        // 初始化索引缓存
        IndexCache.TryAdd(collection.CollectionNamespace.CollectionName, []);

        // 获取现有索引详细信息
        var existingIndexDocs = collection.Indexes.List().ToList();
        var existingIndexes = existingIndexDocs.Select(idx => idx["name"].AsString).ToHashSet();
        var existingIndexDict = existingIndexDocs.ToDictionary(idx => idx["name"].AsString, idx => idx);

        // 处理单字段索引
        var properties = type.GetProperties()
                             .SelectMany(prop => prop.GetCustomAttributes<MongoIndexAttribute>(false)
                                                     .Select(attr => new { Property = prop, Attribute = attr }));
        foreach (var item in properties)
        {
            var fieldName = useCamelCase ? item.Property.Name.ToLowerCamelCase() : item.Property.Name;
            var indexName = item.Attribute.Name ?? $"{fieldName}_{item.Attribute.Type}";
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                // 比较索引定义
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                var expectedKey = item.Attribute.Type switch
                {
                    EIndexType.Ascending   => new(fieldName, 1),
                    EIndexType.Descending  => new(fieldName, -1),
                    EIndexType.Geo2D       => new(fieldName, "2d"),
                    EIndexType.Geo2DSphere => new(fieldName, "2dsphere"),
                    EIndexType.Hashed      => new(fieldName, "hashed"),
                    EIndexType.Text        => new BsonDocument(fieldName, "text"),
                    _                      => throw new NotSupportedException($"Index type {item.Attribute.Type} is not supported")
                };
                if (keys.Equals(expectedKey) && unique == item.Attribute.Unique)
                {
                    needCreate = false; // 定义一致无需重建
                }
                else
                {
                    // 定义不一致，先删除再重建
                    collection.Indexes.DropOne(indexName);
                }
            }
            if (!needCreate)
            {
                continue;
            }
            var builder = Builders<BsonDocument>.IndexKeys;
            var indexDefinition = item.Attribute.Type switch
            {
                EIndexType.Ascending   => builder.Ascending(fieldName),
                EIndexType.Descending  => builder.Descending(fieldName),
                EIndexType.Geo2D       => builder.Geo2D(fieldName),
                EIndexType.Geo2DSphere => builder.Geo2DSphere(fieldName),
                EIndexType.Hashed      => builder.Hashed(fieldName),
                EIndexType.Text        => builder.Text(fieldName),
                _                      => throw new NotSupportedException($"Index type {item.Attribute.Type} is not supported")
            };
            var options = new CreateIndexOptions
            {
                Name = indexName,
                Unique = item.Attribute.Unique,
                Background = true
            };
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexDefinition, options));
            IndexCache[collection.CollectionNamespace.CollectionName].Add(indexName);
        }

        // 处理复合索引
        var compoundIndexes = type.GetCustomAttributes<MongoCompoundIndexAttribute>(false);
        foreach (var index in compoundIndexes)
        {
            var fields = index.Fields.Select(f => useCamelCase ? f.ToLowerCamelCase() : f).ToArray();
            var indexName = index.Name ?? $"compound_{string.Join("_", fields)}";
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                // 构造期望的key文档
                var expectedKey = new BsonDocument();
                for (var i = 0; i < fields.Length; i++)
                {
                    object typeVal = index.Types[i] switch
                    {
                        EIndexType.Ascending   => 1,
                        EIndexType.Descending  => -1,
                        EIndexType.Geo2D       => "2d",
                        EIndexType.Geo2DSphere => "2dsphere",
                        EIndexType.Hashed      => "hashed",
                        EIndexType.Text        => "text",
                        _                      => throw new NotSupportedException($"Index type {index.Types[i]} is not supported")
                    };
                    expectedKey.Add(fields[i], BsonValue.Create(typeVal));
                }
                if (keys.Equals(expectedKey) && unique == index.Unique)
                {
                    needCreate = false;
                }
                else
                {
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
                definitions[i] = index.Types[i] switch
                {
                    EIndexType.Ascending   => builder.Ascending(fields[i]),
                    EIndexType.Descending  => builder.Descending(fields[i]),
                    EIndexType.Geo2D       => builder.Geo2D(fields[i]),
                    EIndexType.Geo2DSphere => builder.Geo2DSphere(fields[i]),
                    EIndexType.Hashed      => builder.Hashed(fields[i]),
                    EIndexType.Text        => builder.Text(fields[i]),
                    _                      => throw new NotSupportedException($"Index type {index.Types[i]} is not supported")
                };
            }
            var combinedDefinition = builder.Combine(definitions);
            var options = new CreateIndexOptions
            {
                Name = indexName,
                Unique = index.Unique,
                Background = true
            };
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(combinedDefinition, options));
            IndexCache[collection.CollectionNamespace.CollectionName].Add(indexName);
        }
    }
}