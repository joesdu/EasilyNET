using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Indexing;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
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
    /// 缓存所有带有 <see cref="TimeSeriesCollectionAttribute" /> 特性的时间序列集合类型。
    /// 使用 <see cref="Lazy{T}" /> 进行延迟初始化，默认线程安全，可在多个线程间安全共享。
    /// 作为 <c>static</c> 字段，该缓存在整个应用程序生命周期内仅初始化一次，并在所有
    /// <see cref="MongoContext" /> 实例之间复用，以减少重复的反射扫描开销。
    /// </summary>
    private static readonly Lazy<HashSet<Type>> TimeSeriesTypes = new(() => [.. AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

    /// <summary>
    /// 对标记 MongoContext 的实体对象，自动创建 MongoDB 索引
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    public static IApplicationBuilder UseCreateMongoIndexes<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var serviceProvider = app.ApplicationServices;
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        var logger = scope.ServiceProvider.GetService<ILogger<T>>();
        // 获取MongoOptions配置
        var options = scope.ServiceProvider.GetRequiredService<BasicClientOptions>();
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
        EnsureIndexes(db, useCamelCase, logger, options);
        return app;
    }

    private static void EnsureIndexes<T>(T dbContext, bool useCamelCase, ILogger? logger, BasicClientOptions options) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        var dbContextType = dbContext.GetType().DeclaringType ?? dbContext.GetType();

        // 缓存反射结果
        if (!PropertyCache.TryGetValue(dbContextType, out var properties))
        {
            properties =
            [
                .. AssemblyHelper.FindTypes(t => t == dbContextType)
                                 .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                 .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>))
            ];
            PropertyCache.TryAdd(dbContextType, properties);
        }
        // 预先获取所有集合信息，避免循环中多次查询
        var collectionOptions = dbContext.Database.ListCollections().ToEnumerable().ToDictionary(doc => doc["name"].AsString,
            doc => doc.Contains("options") && doc["options"].AsBsonDocument.Contains("timeseries"));
        foreach (var prop in properties)
        {
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            if (TimeSeriesTypes.Value.Contains(entityType))
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
            IMongoCollection<BsonDocument>? collection = null;
            if (prop.GetValue(dbContext) is IMongoCollection<BsonDocument> c)
            {
                collection = c;
                collectionName = c.CollectionNamespace.CollectionName;
            }
            else if (prop.GetValue(dbContext) is not null)
            {
                var collectionNameProp = prop.PropertyType.GetProperty(nameof(IMongoCollection<>.CollectionNamespace));
                var collectionNamespace = collectionNameProp?.GetValue(prop.GetValue(dbContext));
                var nameProp = collectionNamespace?.GetType().GetProperty(nameof(IMongoCollection<>.CollectionNamespace.CollectionName));
                collectionName = nameProp?.GetValue(collectionNamespace)?.ToString();
                if (!string.IsNullOrEmpty(collectionName))
                {
                    collection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
                }
            }
            else
            {
                continue;
            }
            if (string.IsNullOrEmpty(collectionName) || collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (CollectionCache.TryAdd(collectionName, 0))
            {
                // 检查集合是否已存在于数据库中
                if (!collectionOptions.ContainsKey(collectionName))
                {
                    try
                    {
                        dbContext.Database.CreateCollection(collectionName);
                        if (logger is not null && logger.IsEnabled(LogLevel.Information))
                        {
                            logger.LogInformation("Created collection {CollectionName}.", collectionName);
                        }
                        // 更新本地缓存的集合信息
                        collectionOptions[collectionName] = false;
                    }
                    catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists")
                    {
                        // 忽略集合已存在的异常
                    }
                }
            }
            var isTimeSeries = collectionOptions.TryGetValue(collectionName, out var isTs) && isTs;
            EnsureIndexesForCollection(collection!, entityType, useCamelCase, logger, isTimeSeries, options);
        }
    }

    private static void EnsureIndexesForCollection(IMongoCollection<BsonDocument> collection, Type type, bool useCamelCase, ILogger? logger, bool isTimeSeries, BasicClientOptions options)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;

        // 获取时序字段信息
        var timeSeriesFields = IndexFieldCollector.GetTimeSeriesFields(type);
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
            var existingIndexes = IndexManager.GetExistingIndexes(collection, logger);
            // 2. 生成当前类型需要的所需索引定义
            var requiredIndexes = IndexDefinitionFactory.GenerateRequiredIndexes(type, collectionName, useCamelCase, isTimeSeries, timeSeriesFields);
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Generated {Count} required indexes for type {TypeName}.", requiredIndexes.Count, type.Name);
            }
            // 3. 比对索引并执行相应操作
            IndexManager.ManageIndexes(collection, existingIndexes, requiredIndexes, logger, options);
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
}