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
                LogExistingCollectionStatus(db, collectionName, attribute, logger);
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

    private static void LogExistingCollectionStatus(IMongoDatabase db, string collectionName, CappedCollectionAttribute attribute, ILogger? logger)
    {
        try
        {
            var collections = db.ListCollections(new ListCollectionsOptions { Filter = new BsonDocument("name", collectionName) }).ToList();
            var collectionInfo = collections.FirstOrDefault();
            if (collectionInfo is null)
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
                return;
            }
            var optionsValue = collectionInfo.GetValue("options", BsonNull.Value);
            if (!optionsValue.IsBsonDocument)
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Collection {CollectionName} already exists but options metadata is unavailable. Expected capped settings: MaxSize={MaxSize}, MaxDocuments={MaxDocuments}.",
                        collectionName, attribute.MaxSize, attribute.MaxDocuments?.ToString() ?? "unlimited");
                }
                return;
            }
            var options = optionsValue.AsBsonDocument;
            var isCapped = options.TryGetValue("capped", out var cappedValue) && cappedValue.ToBoolean();
            var hasMaxSize = TryGetInt64Option(options, "size", out var existingMaxSize);
            var hasMaxDocuments = TryGetInt64Option(options, "max", out var existingMaxDocuments);
            List<string> mismatches = [];
            if (!isCapped)
            {
                mismatches.Add("existing collection is not capped");
            }
            else
            {
                if (!hasMaxSize)
                {
                    mismatches.Add("existing maxSize is unavailable");
                }
                else if (existingMaxSize != attribute.MaxSize)
                {
                    mismatches.Add($"maxSize mismatch (existing={existingMaxSize}, expected={attribute.MaxSize})");
                }
                if (attribute.MaxDocuments.HasValue)
                {
                    if (!hasMaxDocuments)
                    {
                        mismatches.Add("existing maxDocuments is unavailable");
                    }
                    else if (existingMaxDocuments != attribute.MaxDocuments.Value)
                    {
                        mismatches.Add($"maxDocuments mismatch (existing={existingMaxDocuments}, expected={attribute.MaxDocuments.Value})");
                    }
                }
                else if (hasMaxDocuments)
                {
                    mismatches.Add($"maxDocuments mismatch (existing={existingMaxDocuments}, expected=unlimited)");
                }
            }
            if (mismatches.Count > 0)
            {
                if (logger is not null && logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Collection {CollectionName} already exists but does not match capped settings: {MismatchDetails}",
                        collectionName, string.Join("; ", mismatches));
                }
                return;
            }
            if (logger is not null && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Capped collection {CollectionName} already exists and matches expected settings. Skipping creation.", collectionName);
            }
        }
        catch (Exception ex)
        {
            if (logger is not null && logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex,
                    "Collection {CollectionName} already exists, but failed to inspect capped settings. Expected MaxSize={MaxSize}, MaxDocuments={MaxDocuments}.",
                    collectionName, attribute.MaxSize, attribute.MaxDocuments?.ToString() ?? "unlimited");
            }
        }
    }

    private static bool TryGetInt64Option(BsonDocument options, string key, out long value)
    {
        value = 0;
        if (!options.TryGetValue(key, out var optionValue))
        {
            return false;
        }
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
        switch (optionValue.BsonType)
        {
            case BsonType.Int32:
                value = optionValue.AsInt32;
                return true;
            case BsonType.Int64:
                value = optionValue.AsInt64;
                return true;
            case BsonType.Double:
                value = (long)optionValue.AsDouble;
                return true;
            case BsonType.Decimal128:
                value = (long)optionValue.AsDecimal128;
                return true;
            default:
                return false;
        }
    }
}