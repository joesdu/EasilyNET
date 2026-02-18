using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for automatically creating MongoDB capped collections</para>
///     <para xml:lang="zh">自动创建 MongoDB 固定大小集合的扩展方法</para>
/// </summary>
public static class CappedCollectionExtensions
{
    // Cache the types with CappedCollectionAttribute to avoid repeated reflection scanning.
    // 缓存带有 CappedCollectionAttribute 的类型，以避免重复的反射扫描。
    private static readonly Lazy<HashSet<Type>> CachedCappedTypes = new(() => [.. AssemblyHelper.FindTypesByAttribute<CappedCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB capped collections for entity objects marked with
    ///     <see cref="CappedCollectionAttribute" />
    ///     </para>
    ///     <para xml:lang="zh">对标记 <see cref="CappedCollectionAttribute" /> 的实体对象，自动创建 MongoDB 固定大小集合</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <see cref="MongoContext" />
    /// </typeparam>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    public static IApplicationBuilder UseCreateMongoCappedCollections<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var serviceProvider = app.ApplicationServices;
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(nameof(CappedCollectionExtensions));
        // Fetch existing collection names for the current database.
        // 获取当前数据库的现有集合名称。
        var existingCollections = new HashSet<string>(db.Database.ListCollectionNames().ToList(), StringComparer.Ordinal);
        EnsureCappedCollections(db.Database, existingCollections, logger);
        return app;
    }

    private static void EnsureCappedCollections(IMongoDatabase db, HashSet<string> existingCollections, ILogger? logger)
    {
        foreach (var type in CachedCappedTypes.Value)
        {
            var attribute = type.GetCustomAttribute<CappedCollectionAttribute>(false);
            if (attribute is null)
            {
                continue;
            }
            var collectionName = attribute.CollectionName;
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipping capped collection creation for type {TypeName}: collection name is empty.", type.Name);
                }
                continue;
            }
            if (collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipping capped collection creation with system-reserved name: {CollectionName}", collectionName);
                }
                continue;
            }
            if (existingCollections.Contains(collectionName))
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Capped collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
                continue;
            }
            try
            {
                var options = new CreateCollectionOptions
                {
                    Capped = true,
                    MaxSize = attribute.MaxSize,
                    MaxDocuments = attribute.MaxDocuments
                };
                db.CreateCollection(collectionName, options);
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Successfully created capped collection: {CollectionName} (MaxSize={MaxSize}, MaxDocuments={MaxDocuments}).",
                        collectionName, attribute.MaxSize, attribute.MaxDocuments?.ToString() ?? "unlimited");
                }
                existingCollections.Add(collectionName);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists" || ex.Message.Contains("already exists"))
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Capped collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
                existingCollections.Add(collectionName);
            }
            catch (Exception ex)
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Failed to create capped collection: {CollectionName}", collectionName);
                }
            }
        }
    }
}