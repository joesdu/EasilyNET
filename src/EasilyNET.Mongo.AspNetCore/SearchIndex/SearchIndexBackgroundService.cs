using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.SearchIndex;

/// <summary>
///     <para xml:lang="en">
///     Background service that automatically creates MongoDB Atlas Search and Vector Search indexes
///     for entity objects marked with <see cref="MongoSearchIndexAttribute" />.
///     Runs once at application startup and then completes.
///     Requires MongoDB Atlas or MongoDB 8.2+ Community Edition.
///     </para>
///     <para xml:lang="zh">
///     后台服务，自动为标记了 <see cref="MongoSearchIndexAttribute" /> 的实体对象创建 MongoDB Atlas Search 和 Vector Search 索引。
///     在应用启动时运行一次后完成。
///     需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <see cref="MongoContext" />
/// </typeparam>
internal sealed class SearchIndexBackgroundService<T>(IServiceProvider serviceProvider, ILogger<SearchIndexBackgroundService<T>> logger) : BackgroundService where T : MongoContext
{
    // Cache the types with MongoSearchIndexAttribute to avoid repeated reflection scanning.
    private readonly Lazy<HashSet<Type>> CachedSearchIndexTypes = new(static () =>
        [.. AssemblyHelper.FindTypesByAttribute<MongoSearchIndexAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Yield to let the host finish startup before doing potentially slow I/O.
        await Task.Yield();
        try
        {
            var options = serviceProvider.GetRequiredService<BasicClientOptions>();
            var useCamelCase =
                options is { DefaultConventionRegistry: true, ConventionRegistry.Values.Count: 0 } ||
                options.ConventionRegistry.Values.Any(pack => pack.Conventions.Any(c => c is CamelCaseElementNameConvention));
            using var scope = serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetService<T>();
            if (db is null)
            {
                logger.LogWarning("Could not resolve {DbContext} from service provider. Search index creation skipped.", typeof(T).Name);
                return;
            }
            await EnsureSearchIndexesAsync(db, useCamelCase, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Search index creation was cancelled during application shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to ensure search indexes.");
        }
    }

    private async Task EnsureSearchIndexesAsync(MongoContext dbContext, bool useCamelCase, CancellationToken ct)
    {
        var dbContextType = dbContext.GetType().DeclaringType ?? dbContext.GetType();
        var properties = AssemblyHelper.FindTypes(t => t == dbContextType)
                                       .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                       .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>))
                                       .ToArray();
        // Track entity types already processed via DbContext properties to avoid duplicate work in assembly scanning.
        var processedEntityTypes = new HashSet<Type>();
        foreach (var prop in properties)
        {
            ct.ThrowIfCancellationRequested();
            var entityType = prop.PropertyType.GetGenericArguments()[0];
            var searchIndexAttrs = entityType.GetCustomAttributes<MongoSearchIndexAttribute>(false).ToList();
            if (searchIndexAttrs.Count == 0)
            {
                continue;
            }
            // Resolve collection name
            var collectionName = ResolveCollectionName(dbContext, prop);
            if (string.IsNullOrEmpty(collectionName))
            {
                continue;
            }
            processedEntityTypes.Add(entityType);
            await EnsureSearchIndexesForCollectionAsync(dbContext, entityType, collectionName, searchIndexAttrs, useCamelCase, ct).ConfigureAwait(false);
        }
        // Also check types found via assembly scanning (not just DbContext properties).
        // Types that specify CollectionName on the attribute can have indexes created without being declared on the DbContext.
        foreach (var type in CachedSearchIndexTypes.Value)
        {
            ct.ThrowIfCancellationRequested();
            if (processedEntityTypes.Contains(type))
            {
                continue;
            }
            var searchIndexAttrs = type.GetCustomAttributes<MongoSearchIndexAttribute>(false).ToList();
            if (searchIndexAttrs.Count == 0)
            {
                continue;
            }
            // Group attributes by CollectionName. Attributes with CollectionName set can be processed;
            // attributes without CollectionName cannot be resolved and will be warned about.
            var attrsWithCollection = searchIndexAttrs.Where(a => !string.IsNullOrWhiteSpace(a.CollectionName)).ToList();
            var attrsWithoutCollection = searchIndexAttrs.Where(a => string.IsNullOrWhiteSpace(a.CollectionName)).ToList();
            if (attrsWithoutCollection.Count > 0 && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Type {TypeName} has {Count} MongoSearchIndexAttribute(s) without CollectionName. " +
                                  "These indexes cannot be created via assembly scanning. Either declare the type as an IMongoCollection<T> property " +
                                  "on the MongoContext, or set CollectionName on the attribute.",
                    type.Name, attrsWithoutCollection.Count);
            }
            if (attrsWithCollection.Count == 0)
            {
                continue;
            }
            // Group by CollectionName and create indexes for each collection.
            foreach (var group in attrsWithCollection.GroupBy(a => a.CollectionName!))
            {
                ct.ThrowIfCancellationRequested();
                await EnsureSearchIndexesForCollectionAsync(dbContext, type, group.Key, [.. group], useCamelCase, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureSearchIndexesForCollectionAsync(MongoContext dbContext, Type entityType, string collectionName, List<MongoSearchIndexAttribute> searchIndexAttrs, bool useCamelCase, CancellationToken ct)
    {
        var collection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
        // Get existing search indexes
        var existingIndexes = await SearchIndexManager.GetExistingSearchIndexesAsync(collection, logger, ct).ConfigureAwait(false);
        foreach (var indexAttr in searchIndexAttrs)
        {
            ct.ThrowIfCancellationRequested();
            if (existingIndexes.ContainsKey(indexAttr.Name))
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("Search index {IndexName} already exists on collection {CollectionName}. Skipping.",
                        indexAttr.Name, collectionName);
                }
                continue;
            }
            var indexType = indexAttr.Type == ESearchIndexType.VectorSearch ? SearchIndexType.VectorSearch : SearchIndexType.Search;
            var definition = indexAttr.Type == ESearchIndexType.VectorSearch
                                 ? SearchIndexDefinitionFactory.GenerateVectorSearchDefinition(entityType, indexAttr, useCamelCase)
                                 : SearchIndexDefinitionFactory.GenerateSearchDefinition(entityType, indexAttr, useCamelCase);
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Creating search index {IndexName} (type={IndexType}) on collection {CollectionName}.",
                    indexAttr.Name, indexType, collectionName);
            }
            await SearchIndexManager.CreateSearchIndexAsync(collection, indexAttr.Name, indexType, definition, logger, ct).ConfigureAwait(false);
        }
    }

    private static string? ResolveCollectionName(MongoContext dbContext, PropertyInfo prop)
    {
        var value = prop.GetValue(dbContext);
        if (value is null)
        {
            return null;
        }
        var collectionNameProp = prop.PropertyType.GetProperty(nameof(IMongoCollection<>.CollectionNamespace));
        var collectionNamespace = collectionNameProp?.GetValue(value);
        var nameProp = collectionNamespace?.GetType().GetProperty(nameof(CollectionNamespace.CollectionName));
        return nameProp?.GetValue(collectionNamespace)?.ToString();
    }
}