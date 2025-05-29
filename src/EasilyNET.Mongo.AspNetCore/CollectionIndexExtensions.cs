using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.AspNetCore.Builder;
using MongoDB.Bson;
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
    private static readonly ConcurrentBag<string> CollectionCache = [];
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
        var collections = db.Database.ListCollectionNames().ToList().Where(c => c.IsNotNullOrWhiteSpace()).ToArray();
        CollectionCache.AddRange(collections);
        EnsureIndexes(db.Database);
        return app;
    }

    private static void EnsureIndexes(IMongoDatabase db)
    {
        // 获取所有DbContext相关类型
        var dbContextType = db.GetType().DeclaringType ?? db.GetType();
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
            // 获取IMongoCollection实例
            var collectionObj = prop.GetValue(db);
            if (collectionObj is IMongoCollection<BsonDocument> collection)
            {
                var collectionName = collection.CollectionNamespace.CollectionName;
                if (string.Equals(collectionName, "system.profile", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var hasCache = CollectionCache.Contains(collectionName);
                if (!hasCache)
                {
                    db.CreateCollection(collectionName);
                    CollectionCache.Add(collectionName);
                }
                EnsureIndexesForCollection(collection, entityType);
            }
            else if (collectionObj is not null)
            {
                // 兼容IMongoCollection<T>不是BsonDocument的情况
                var collectionNameProp = collectionObj.GetType().GetProperty("CollectionNamespace");
                var collectionNamespace = collectionNameProp?.GetValue(collectionObj);
                var nameProp = collectionNamespace?.GetType().GetProperty("CollectionName");
                var collectionName = nameProp?.GetValue(collectionNamespace)?.ToString();
                if (string.IsNullOrEmpty(collectionName) || string.Equals(collectionName, "system.profile", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                var hasCache = CollectionCache.Contains(collectionName);
                if (!hasCache)
                {
                    db.CreateCollection(collectionName);
                    CollectionCache.Add(collectionName);
                }
                // 获取BsonDocument集合
                var bsonCollection = db.GetCollection<BsonDocument>(collectionName);
                EnsureIndexesForCollection(bsonCollection, entityType);
            }
        }
    }

    private static void EnsureIndexesForCollection(IMongoCollection<BsonDocument> collection, Type type)
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
            var indexName = item.Attribute.Name ?? $"{item.Property.Name}_{item.Attribute.Type}";
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                // 比较索引定义
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                var expectedKey = item.Attribute.Type switch
                {
                    EIndexType.Ascending   => new(item.Property.Name, 1),
                    EIndexType.Descending  => new(item.Property.Name, -1),
                    EIndexType.Geo2D       => new(item.Property.Name, "2d"),
                    EIndexType.Geo2DSphere => new(item.Property.Name, "2dsphere"),
                    EIndexType.Hashed      => new(item.Property.Name, "hashed"),
                    EIndexType.Text        => new BsonDocument(item.Property.Name, "text"),
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
                EIndexType.Ascending   => builder.Ascending(item.Property.Name),
                EIndexType.Descending  => builder.Descending(item.Property.Name),
                EIndexType.Geo2D       => builder.Geo2D(item.Property.Name),
                EIndexType.Geo2DSphere => builder.Geo2DSphere(item.Property.Name),
                EIndexType.Hashed      => builder.Hashed(item.Property.Name),
                EIndexType.Text        => builder.Text(item.Property.Name),
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
            var indexName = index.Name ?? $"compound_{string.Join("_", index.Fields)}";
            var needCreate = true;
            if (existingIndexes.Contains(indexName))
            {
                var existing = existingIndexDict[indexName];
                var keys = existing["key"].AsBsonDocument;
                var unique = existing.Contains("unique") && existing["unique"].AsBoolean;
                // 构造期望的key文档
                var expectedKey = new BsonDocument();
                for (var i = 0; i < index.Fields.Length; i++)
                {
                    var field = index.Fields[i];
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
                    expectedKey.Add(field, BsonValue.Create(typeVal));
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
            var definitions = new IndexKeysDefinition<BsonDocument>[index.Fields.Length];
            for (var i = 0; i < index.Fields.Length; i++)
            {
                definitions[i] = index.Types[i] switch
                {
                    EIndexType.Ascending   => builder.Ascending(index.Fields[i]),
                    EIndexType.Descending  => builder.Descending(index.Fields[i]),
                    EIndexType.Geo2D       => builder.Geo2D(index.Fields[i]),
                    EIndexType.Geo2DSphere => builder.Geo2DSphere(index.Fields[i]),
                    EIndexType.Hashed      => builder.Hashed(index.Fields[i]),
                    EIndexType.Text        => builder.Text(index.Fields[i]),
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