using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.AspNetCore.SearchIndex;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for automatically creating MongoDB Atlas Search and Vector Search indexes</para>
///     <para xml:lang="zh">自动创建 MongoDB Atlas Search 和 Vector Search 索引的扩展方法</para>
/// </summary>
public static class SearchIndexExtensions
{
    // Cache the types with MongoSearchIndexAttribute to avoid repeated reflection scanning.
    private static readonly Lazy<HashSet<Type>> CachedSearchIndexTypes = new(() =>
        [.. AssemblyHelper.FindTypesByAttribute<MongoSearchIndexAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB Atlas Search and Vector Search indexes for entity objects marked with
    ///     <see cref="MongoSearchIndexAttribute" />.
    ///     Requires MongoDB Atlas or MongoDB 8.2+ Community Edition.
    ///     On unsupported deployments, this method logs a warning and skips index creation.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     对标记 <see cref="MongoSearchIndexAttribute" /> 的实体对象，自动创建 MongoDB Atlas Search 和 Vector Search 索引。
    ///     需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。
    ///     在不支持的部署上，此方法记录警告并跳过索引创建。
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <see cref="MongoContext" />
    /// </typeparam>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    public static IApplicationBuilder UseCreateMongoSearchIndexes<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var serviceProvider = app.ApplicationServices;
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(SearchIndexExtensions));
        var options = scope.ServiceProvider.GetRequiredService<BasicClientOptions>();
        var useCamelCase =
            options is { DefaultConventionRegistry: true, ConventionRegistry.Values.Count: 0 } ||
            options.ConventionRegistry.Values.Any(pack => pack.Conventions.Any(c => c is CamelCaseElementNameConvention));
        // Fire and forget — search index creation is async on Atlas side anyway
        _ = Task.Run(async () =>
        {
            try
            {
                await EnsureSearchIndexesAsync(db, useCamelCase, logger).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Failed to ensure search indexes.");
            }
        });
        return app;
    }

    private static async Task EnsureSearchIndexesAsync(MongoContext dbContext, bool useCamelCase, ILogger? logger)
    {
        var dbContextType = dbContext.GetType().DeclaringType ?? dbContext.GetType();
        var properties = AssemblyHelper.FindTypes(t => t == dbContextType)
                                       .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                       .Where(prop => prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(IMongoCollection<>))
                                       .ToArray();
        foreach (var prop in properties)
        {
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
            var collection = dbContext.Database.GetCollection<BsonDocument>(collectionName);
            // Get existing search indexes
            var existingIndexes = await SearchIndexManager.GetExistingSearchIndexesAsync(collection, logger).ConfigureAwait(false);
            foreach (var indexAttr in searchIndexAttrs)
            {
                if (existingIndexes.ContainsKey(indexAttr.Name))
                {
                    if (logger is not null && logger.IsEnabled(LogLevel.Debug))
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
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Creating search index {IndexName} (type={IndexType}) on collection {CollectionName}.",
                        indexAttr.Name, indexType, collectionName);
                }
                await SearchIndexManager.CreateSearchIndexAsync(collection, indexAttr.Name, indexType, definition, logger).ConfigureAwait(false);
            }
        }
        // Also check types found via assembly scanning (not just DbContext properties)
        foreach (var type in CachedSearchIndexTypes.Value)
        {
            var searchIndexAttrs = type.GetCustomAttributes<MongoSearchIndexAttribute>(false).ToList();
            if (searchIndexAttrs.Count == 0)
            {
                continue;
            }
            // For assembly-scanned types, we need to find the collection name from the type name
            // This is a fallback — DbContext property-based resolution is preferred
            if (logger is not null && logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Found search index attributes on type {TypeName} via assembly scanning.", type.Name);
            }
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