using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Time series collection extension class</para>
///     <para xml:lang="zh">时间序列集合扩展类</para>
/// </summary>
public static class TimeSeriesCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Do not attempt to create a time series collection or view named system.profile. If you attempt to do so, MongoDB 6.3 and
    ///     later will return an IllegalOperation error. Earlier MongoDB versions will crash as a result.
    ///     </para>
    ///     <para xml:lang="zh">不要尝试创建名称为 system.profile 的时间序列集合或视图。如果您尝试这样做，MongoDB 6.3 及更高版本会返回 IllegalOperation 错误。早期 MongoDB 版本会因此崩溃。</para>
    /// </summary>
    private const string IllegalName = "system.profile";

    private static readonly ConcurrentBag<string> CollectionCache = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB time series collections for entity objects marked with
    ///     <see cref="TimeSeriesCollectionAttribute" />
    ///     </para>
    ///     <para xml:lang="zh">对标记 <see cref="TimeSeriesCollectionAttribute" /> 的实体对象,自动创建 MongoDB 时序集合</para>
    /// </summary>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    public static IApplicationBuilder UseCreateMongoTimeSeriesCollection<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var serviceProvider = app.ApplicationServices;
        Task.Run(() =>
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<T>();
            ArgumentNullException.ThrowIfNull(db, nameof(T));
            var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("TimeSeriesCollectionExtensions");
            try
            {
                var collections = db.Database.ListCollectionNames().ToList();
                // Ensure CollectionCache is thread-safe for additions if accessed by multiple EnsureTimeSeriesCollections calls concurrently.
                // ConcurrentBag is already thread-safe for Add.
                // 确保 CollectionCache 在并发调用 EnsureTimeSeriesCollections 时对于添加操作是线程安全的。
                // ConcurrentBag 本身对于 Add 操作就是线程安全的。
                foreach (var colName in collections.Where(colName => !CollectionCache.Contains(colName)))
                {
                    CollectionCache.Add(colName);
                }
                EnsureTimeSeriesCollections(db.Database, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to create MongoDB time-series collections for context {ContextType} in background task.", typeof(T).Name);
            }
        }).ContinueWith(t =>
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            if (!t.IsFaulted || loggerFactory is null)
            {
                return;
            }
            var globalLogger = loggerFactory.CreateLogger("MongoTimeSeriesCreationTask");
            globalLogger.LogError(t.Exception, "Background task for creating MongoDB time-series collections failed.");
        }, TaskScheduler.Default);
        return app;
    }

    private static void EnsureTimeSeriesCollections(IMongoDatabase db, ILogger? logger)
    {
        var types = AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false);
        foreach (var type in types)
        {
            var tsCollectionAttrs = type.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).ToArray();
            if (tsCollectionAttrs.Length == 0)
            {
                continue;
            }
            var attribute = tsCollectionAttrs[0];
            var collectionName = attribute.CollectionName;
            if (IllegalName.Equals(collectionName, StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogWarning("Skipping creation of time-series collection with illegal name: {CollectionName}", collectionName);
                continue;
            }
            var collectionExists = CollectionCache.Contains(collectionName);
            if (!collectionExists)
            {
                try
                {
                    db.CreateCollection(collectionName, new()
                    {
                        TimeSeriesOptions = attribute.TimeSeriesOptions, // 设置时序选项
                        ExpireAfter = attribute.ExpireAfter              // 设置过期时间
                    });
                    logger?.LogInformation("Successfully created time-series collection: {CollectionName}", collectionName);
                    CollectionCache.Add(collectionName);
                }
                catch (MongoCommandException ex) when (ex.Message.Contains("already exists"))
                {
                    logger?.LogWarning("Time-series collection {CollectionName} already exists. Skipping creation.", collectionName);
                    // Ensure it's in the cache if it already exists // 如果集合已存在，确保它在缓存中
                    if (!CollectionCache.Contains(collectionName))
                    {
                        CollectionCache.Add(collectionName);
                    }
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to create time-series collection: {CollectionName}", collectionName);
                    continue;
                }
            }
            else
            {
                logger?.LogInformation("Time-series collection {CollectionName} already exists. Skipping creation.", collectionName);
            }
            // Indexing for Time-Series Collections // 时序集合的索引
            // MongoDB automatically creates an index on the timeField. // MongoDB 会自动在 timeField 上创建索引。
            // An additional index on (metaField, timeField) is beneficial if metaField is used in queries. // 如果查询中使用了 metaField，则在 (metaField, timeField) 上创建额外索引会很有益。
            if (string.IsNullOrWhiteSpace(attribute.TimeSeriesOptions.MetaField)) // 如果 metaField 为空或空白，则跳过创建 metaField 索引
            {
                continue;
            }
            var metaFieldName = attribute.TimeSeriesOptions.MetaField;       // 获取 metaField 名称
            var timeFieldName = attribute.TimeSeriesOptions.TimeField;       // Should always be present // 获取 timeField 名称（应始终存在）
            var collection = db.GetCollection<BsonDocument>(collectionName); // 获取集合对象
            var indexName = $"idx_{metaFieldName}_{timeFieldName}";          // 生成索引名称
            // Check if a similar index already exists to avoid errors or redundant operations // 检查是否存在类似的索引，以避免错误或冗余操作
            var indexExists = false;
            try
            {
                using var cursor = collection.Indexes.List();
                foreach (var indexDocument in cursor.ToEnumerable())
                {
                    if (indexDocument.TryGetValue("name", out var name) && name.IsString && name.AsString == indexName)
                    {
                        indexExists = true;
                        break;
                    }
                    // More robust check: verify key structure if names can vary // 更可靠的检查：如果名称可能不同，则验证键结构
                    if (!indexDocument.TryGetValue("key", out var keyDoc) || !keyDoc.IsBsonDocument)
                    {
                        continue;
                    }
                    var keys = keyDoc.AsBsonDocument;
                    // ReSharper disable once InvertIf
                    if (keys.Contains(metaFieldName) &&     // 检查键是否包含 metaField
                        keys[metaFieldName].AsInt32 == 1 && // 检查 metaField 的索引方向是否为升序
                        keys.Contains(timeFieldName) &&     // 检查键是否包含 timeField
                        keys[timeFieldName].AsInt32 == 1 && // or -1 depending on desired sort for timeField in this compound index // 检查 timeField 的索引方向是否为升序（或根据需要为降序）
                        keys.ElementCount == 2)             // 检查是否为包含两个字段的复合索引
                    {
                        indexExists = true;
                        // Potentially update indexName if a matching key structure is found with a different name
                        // For simplicity, we assume if keys match, the intent is met.
                        // 如果找到具有不同名称但键结构匹配的索引，则可能需要更新 indexName
                        // 为简单起见，我们假设如果键匹配，则意图已满足。
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to list indexes for collection {CollectionName} when checking for metaField index.", collectionName);
            }
            if (!indexExists)
            {
                try
                {
                    var indexKeysDefinition = Builders<BsonDocument>.IndexKeys.Ascending(metaFieldName).Ascending(timeFieldName);
                    var createIndexOptions = new CreateIndexOptions { Name = indexName, Background = true };
                    collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeysDefinition, createIndexOptions));
                    logger?.LogInformation("Successfully created index {IndexName} on metaField and timeField for collection {CollectionName}.", indexName, collectionName);
                }
                catch (MongoCommandException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("would create a duplicate index"))
                {
                    logger?.LogWarning("Index {IndexName} or a similar one on metaField and timeField for collection {CollectionName} already exists.", indexName, collectionName);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to create index on metaField and timeField for collection {CollectionName}.", collectionName);
                }
            }
            else
            {
                logger?.LogInformation("Index on metaField and timeField (or similar) already exists for collection {CollectionName}. Skipping creation.", collectionName);
            }
        }
    }
}