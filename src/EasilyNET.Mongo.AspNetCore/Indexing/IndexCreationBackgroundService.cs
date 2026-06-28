using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Helpers;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Indexing;

/// <summary>
///     <para xml:lang="en">
///     Background service that automatically creates MongoDB indexes for entity objects marked with
///     <see cref="MongoIndexAttribute" /> / <see cref="MongoCompoundIndexAttribute" />.
///     Runs once at application startup (without blocking startup) using the async driver APIs and then completes.
///     </para>
///     <para xml:lang="zh">
///     后台服务，自动为标记了 <see cref="MongoIndexAttribute" /> / <see cref="MongoCompoundIndexAttribute" /> 的实体对象创建 MongoDB 索引。
///     在应用启动时运行一次（不阻塞启动），全程使用异步驱动 API，完成后结束。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <see cref="MongoContext" />
/// </typeparam>
internal sealed class IndexCreationBackgroundService<T>(IServiceProvider serviceProvider, ILogger<IndexCreationBackgroundService<T>> logger) : BackgroundService where T : MongoContext
{
    // 已处理过创建动作的集合名缓存，避免跨上下文/重复执行时重复创建集合。
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ConcurrentDictionary<string, byte> CollectionCache = [];

    // 缓存 DbContext 的 IMongoCollection<> 属性，避免每次启动重复反射。
    // ReSharper disable once StaticMemberInGenericType
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    // 缓存带有 TimeSeriesCollectionAttribute 的类型，时序集合的索引由其它路径处理，此处跳过。
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lazy<HashSet<Type>> TimeSeriesTypes = new(static () =>
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
                logger.LogWarning("Could not resolve {DbContext} from service provider. Index creation skipped.", typeof(T).Name);
                return;
            }
            var options = scope.ServiceProvider.GetRequiredService<BasicClientOptions>();
            await EnsureIndexesAsync(db, MongoServiceExtensionsHelpers.UseCamelCase, options, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Index creation was cancelled during application shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create MongoDB indexes for context {ContextType}.", typeof(T).Name);
        }
    }

    private async Task EnsureIndexesAsync(MongoContext dbContext, bool useCamelCase, BasicClientOptions options, CancellationToken ct)
    {
        var dbContextType = dbContext.GetType();
        var properties = PropertyCache.GetOrAdd(dbContextType, static type =>
        [
            .. AssemblyHelper.FindTypes(t => t == type)
                             .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                             .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>))
        ]);
        // 预先获取所有集合信息（name -> 是否为时序集合），避免循环中多次查询。
        using var collectionsCursor = await dbContext.Database.ListCollectionsAsync(cancellationToken: ct).ConfigureAwait(false);
        var collectionDocs = await collectionsCursor.ToListAsync(ct).ConfigureAwait(false);
        var collectionOptions = collectionDocs.ToDictionary(doc => doc["name"].AsString,
            doc => doc.Contains("options") && doc["options"].AsBsonDocument.Contains("timeseries"));
        // 预热集合名缓存（跳过 system.* 系统集合）。
        foreach (var name in collectionOptions.Keys.Where(n => !n.StartsWith("system.", StringComparison.OrdinalIgnoreCase)))
        {
            CollectionCache.TryAdd(name, 0);
        }
        foreach (var prop in properties)
        {
            ct.ThrowIfCancellationRequested();
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
            // 仅在该集合名首次出现（即数据库中尚不存在且本次未处理过）时尝试创建集合。
            if (CollectionCache.TryAdd(collectionName, 0) && !collectionOptions.ContainsKey(collectionName))
            {
                try
                {
                    await dbContext.Database.CreateCollectionAsync(collectionName, cancellationToken: ct).ConfigureAwait(false);
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("Created collection {CollectionName}.", collectionName);
                    }
                    collectionOptions[collectionName] = false;
                }
                catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists")
                {
                    // 忽略集合已存在的异常
                }
            }
            var isTimeSeries = collectionOptions.TryGetValue(collectionName, out var isTs) && isTs;
            if (collection is null)
            {
                continue;
            }
            await EnsureIndexesForCollectionAsync(collection, entityType, useCamelCase, isTimeSeries, options, ct).ConfigureAwait(false);
        }
    }

    private async Task EnsureIndexesForCollectionAsync(IMongoCollection<BsonDocument> collection, Type type, bool useCamelCase, bool isTimeSeries, BasicClientOptions options, CancellationToken ct)
    {
        var collectionName = collection.CollectionNamespace.CollectionName;
        // 获取时序字段信息
        var timeSeriesFields = IndexFieldCollector.GetTimeSeriesFields(type);
        if (isTimeSeries && timeSeriesFields.Count > 0 && logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Detected time-series collection {CollectionName}. Time fields [{TimeFields}] will be excluded from indexing.", collectionName, string.Join(", ", timeSeriesFields));
        }
        try
        {
            // 1. 查询数据库中现有的所有索引
            var existingIndexes = await IndexManager.GetExistingIndexesAsync(collection, logger, ct).ConfigureAwait(false);
            // 2. 生成当前类型需要的所需索引定义
            var requiredIndexes = IndexDefinitionFactory.GenerateRequiredIndexes(type, collectionName, useCamelCase, isTimeSeries, timeSeriesFields);
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Generated {Count} required indexes for type {TypeName}.", requiredIndexes.Count, type.Name);
            }
            // 3. 比对索引并执行相应操作
            await IndexManager.ManageIndexesAsync(collection, existingIndexes, requiredIndexes, logger, options, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "Failed to manage indexes for collection {CollectionName}.", collectionName);
            }
            throw;
        }
    }
}
