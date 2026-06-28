using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.Collections;

/// <summary>
///     <para xml:lang="en">
///     Background service that automatically creates MongoDB time series collections for entity objects marked with
///     <see cref="TimeSeriesCollectionAttribute" />. Runs once at application startup (without blocking startup) using the
///     async driver APIs and then completes.
///     </para>
///     <para xml:lang="zh">
///     后台服务，自动为标记了 <see cref="TimeSeriesCollectionAttribute" /> 的实体对象创建 MongoDB 时序集合。
///     在应用启动时运行一次（不阻塞启动），全程使用异步驱动 API，完成后结束。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <see cref="MongoContext" />
/// </typeparam>
internal sealed class TimeSeriesCollectionBackgroundService<T>(IServiceProvider serviceProvider, ILogger<TimeSeriesCollectionBackgroundService<T>> logger) : BackgroundService where T : MongoContext
{
    /// <summary>
    /// 不要尝试创建名称为 system.profile 的时序集合或视图，MongoDB 6.3+ 会返回 IllegalOperation 错误，早期版本会崩溃。
    /// </summary>
    private const string IllegalName = "system.profile";

    // Cache the types with TimeSeriesCollectionAttribute to avoid repeated reflection scanning.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lazy<HashSet<Type>> CachedTimeSeriesTypes = new(static () =>
        [.. AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 让出，确保不在 StartAsync 同步阶段执行，从而不阻塞应用启动。
        await Task.Yield();
        try
        {
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<T>();
            if (db is null)
            {
                logger.LogWarning("Could not resolve {DbContext} from service provider. Time-series collection creation skipped.", typeof(T).Name);
                return;
            }
            // 获取当前数据库的现有集合名称。
            using var cursor = await db.Database.ListCollectionNamesAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            var names = await cursor.ToListAsync(stoppingToken).ConfigureAwait(false);
            var existingCollections = new HashSet<string>(names, StringComparer.Ordinal);
            await EnsureTimeSeriesCollectionsAsync(db.Database, existingCollections, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Time-series collection creation was cancelled during application shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create MongoDB time-series collections for context {ContextType}.", typeof(T).Name);
        }
    }

    private async Task EnsureTimeSeriesCollectionsAsync(IMongoDatabase db, HashSet<string> existingCollections, CancellationToken ct)
    {
        foreach (var type in CachedTimeSeriesTypes.Value)
        {
            ct.ThrowIfCancellationRequested();
            var attribute = type.GetCustomAttribute<TimeSeriesCollectionAttribute>(false);
            if (attribute is null)
            {
                continue;
            }
            var collectionName = attribute.CollectionName;
            if (IllegalName.Equals(collectionName, StringComparison.OrdinalIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipping creation of time-series collection with illegal name: {CollectionName}", collectionName);
                }
                continue;
            }
            if (!existingCollections.Contains(collectionName))
            {
                try
                {
                    await db.CreateCollectionAsync(collectionName, new()
                    {
                        TimeSeriesOptions = attribute.TimeSeriesOptions,                                             // 设置时序选项
                        ExpireAfter = attribute.ExpireAfter < 0 ? null : TimeSpan.FromSeconds(attribute.ExpireAfter) // 设置过期时间
                    }, ct).ConfigureAwait(false);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Successfully created time-series collection: {CollectionName}", collectionName);
                    }
                    existingCollections.Add(collectionName);
                }
                catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists" || ex.Message.Contains("already exists"))
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Time-series collection {CollectionName} already exists. Skipping creation.", collectionName);
                    }
                    existingCollections.Add(collectionName);
                }
                catch (Exception ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Failed to create time-series collection: {CollectionName}", collectionName);
                    }
                    continue;
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Time-series collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
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
                using var indexCursor = await collection.Indexes.ListAsync(cancellationToken: ct).ConfigureAwait(false);
                var indexDocuments = await indexCursor.ToListAsync(ct).ConfigureAwait(false);
                foreach (var indexDocument in indexDocuments)
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
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Failed to list indexes for collection {CollectionName} when checking for metaField index.", collectionName);
                }
            }
            if (!indexExists)
            {
                try
                {
                    var indexKeysDefinition = Builders<BsonDocument>.IndexKeys.Ascending(metaFieldName).Ascending(timeFieldName);
                    // Background is a no-op since MongoDB 4.2; omitted intentionally.
                    var createIndexOptions = new CreateIndexOptions { Name = indexName };
                    await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(indexKeysDefinition, createIndexOptions), cancellationToken: ct).ConfigureAwait(false);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Successfully created index {IndexName} on metaField and timeField for collection {CollectionName}.", indexName, collectionName);
                    }
                }
                catch (MongoCommandException ex) when (ex.CodeName == "IndexOptionsConflict" || ex.CodeName == "IndexKeySpecsConflict" || ex.Message.Contains("already exists"))
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Index {IndexName} or a similar one on metaField and timeField for collection {CollectionName} already exists.", indexName, collectionName);
                    }
                }
                catch (Exception ex)
                {
                    if (logger.IsEnabled(LogLevel.Error))
                    {
                        logger.LogError(ex, "Failed to create index on metaField and timeField for collection {CollectionName}.", collectionName);
                    }
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Index on metaField and timeField (or similar) already exists for collection {CollectionName}. Skipping creation.", collectionName);
                }
            }
        }
    }
}
