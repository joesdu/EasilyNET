using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using EasilyNET.Mongo.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Collections;

/// <summary>
///     <para xml:lang="en">
///     Background service that automatically creates MongoDB capped collections for entity objects marked with
///     <see cref="CappedCollectionAttribute" />. Runs once at application startup (without blocking startup) using the
///     async driver APIs and then completes.
///     </para>
///     <para xml:lang="zh">
///     后台服务，自动为标记了 <see cref="CappedCollectionAttribute" /> 的实体对象创建 MongoDB 固定大小集合。
///     在应用启动时运行一次（不阻塞启动），全程使用异步驱动 API，完成后结束。
///     </para>
/// </summary>
/// <typeparam name="T">
///     <see cref="MongoContext" />
/// </typeparam>
internal sealed class CappedCollectionBackgroundService<T>(IServiceProvider serviceProvider, ILogger<CappedCollectionBackgroundService<T>> logger) : BackgroundService where T : MongoContext
{
    // Cache the types with CappedCollectionAttribute to avoid repeated reflection scanning.
    // ReSharper disable once StaticMemberInGenericType
    private static readonly Lazy<HashSet<Type>> CachedCappedTypes = new(static () =>
        [.. AssemblyHelper.FindTypesByAttribute<CappedCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false }, false)]);

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
                logger.LogWarning("Could not resolve {DbContext} from service provider. Capped collection creation skipped.", typeof(T).Name);
                return;
            }
            // 获取当前数据库的现有集合名称。
            using var cursor = await db.Database.ListCollectionNamesAsync(cancellationToken: stoppingToken).ConfigureAwait(false);
            var names = await cursor.ToListAsync(stoppingToken).ConfigureAwait(false);
            var existingCollections = new HashSet<string>(names, StringComparer.Ordinal);
            await EnsureCappedCollectionsAsync(db.Database, existingCollections, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Capped collection creation was cancelled during application shutdown.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create MongoDB capped collections for context {ContextType}.", typeof(T).Name);
        }
    }

    private async Task EnsureCappedCollectionsAsync(IMongoDatabase db, HashSet<string> existingCollections, CancellationToken ct)
    {
        foreach (var type in CachedCappedTypes.Value)
        {
            ct.ThrowIfCancellationRequested();
            var attribute = type.GetCustomAttribute<CappedCollectionAttribute>(false);
            if (attribute is null)
            {
                continue;
            }
            var collectionName = attribute.CollectionName;
            if (string.IsNullOrWhiteSpace(collectionName))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipping capped collection creation for type {TypeName}: collection name is empty.", type.Name);
                }
                continue;
            }
            if (collectionName.StartsWith("system.", StringComparison.OrdinalIgnoreCase))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipping capped collection creation with system-reserved name: {CollectionName}", collectionName);
                }
                continue;
            }
            if (existingCollections.Contains(collectionName))
            {
                await LogExistingCollectionStatusAsync(db, collectionName, attribute, ct).ConfigureAwait(false);
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
                await db.CreateCollectionAsync(collectionName, options, ct).ConfigureAwait(false);
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Successfully created capped collection: {CollectionName} (MaxSize={MaxSize}, MaxDocuments={MaxDocuments}).",
                        collectionName, attribute.MaxSize, attribute.MaxDocuments?.ToString() ?? "unlimited");
                }
                existingCollections.Add(collectionName);
            }
            catch (MongoCommandException ex) when (ex.CodeName == "NamespaceExists" || ex.Message.Contains("already exists"))
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Capped collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
                existingCollections.Add(collectionName);
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "Failed to create capped collection: {CollectionName}", collectionName);
                }
            }
        }
    }

    private async Task LogExistingCollectionStatusAsync(IMongoDatabase db, string collectionName, CappedCollectionAttribute attribute, CancellationToken ct)
    {
        try
        {
            using var cursor = await db.ListCollectionsAsync(new() { Filter = new BsonDocument("name", collectionName) }, ct).ConfigureAwait(false);
            var collections = await cursor.ToListAsync(ct).ConfigureAwait(false);
            var collectionInfo = collections.FirstOrDefault();
            if (collectionInfo is null)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Collection {CollectionName} already exists. Skipping creation.", collectionName);
                }
                return;
            }
            var optionsValue = collectionInfo.GetValue("options", BsonNull.Value);
            if (!optionsValue.IsBsonDocument)
            {
                if (logger.IsEnabled(LogLevel.Warning))
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
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Collection {CollectionName} already exists but does not match capped settings: {MismatchDetails}",
                        collectionName, string.Join("; ", mismatches));
                }
                return;
            }
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Capped collection {CollectionName} already exists and matches expected settings. Skipping creation.", collectionName);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Warning))
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
