using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Conventions;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Helpers;

internal static class MongoServiceExtensionsHelpers
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Thread-safe flag to ensure convention packs and global serializers are registered only once.
    ///     </para>
    ///     <para xml:lang="zh">线程安全标志，确保 convention pack 和全局序列化器仅注册一次。</para>
    /// </summary>
    private static int _conventionsRegistered;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Indicates whether the CamelCase element naming convention is active globally.
    ///     Set during convention registration and used by index/search-index creation to determine field name casing.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     指示全局是否启用了驼峰命名约定。在 Convention 注册时设置，供索引/搜索索引创建时判断字段名大小写。
    ///     </para>
    /// </summary>
    internal static bool UseCamelCase { get; private set; }

    /// <summary>
    /// Applies resilience options to the MongoDB client settings.
    /// </summary>
    /// <param name="settings"></param>
    /// <param name="resilience"></param>
    internal static void ApplyResilienceOptions(MongoClientSettings settings, MongoResilienceOptions resilience)
    {
        if (!resilience.Enable)
        {
            return;
        }
        settings.ServerSelectionTimeout = resilience.ServerSelectionTimeout;
        settings.ConnectTimeout = resilience.ConnectTimeout;
        settings.SocketTimeout = resilience.SocketTimeout;
        settings.WaitQueueTimeout = resilience.WaitQueueTimeout;
        settings.HeartbeatInterval = resilience.HeartbeatInterval;
        settings.MaxConnectionPoolSize = resilience.MaxConnectionPoolSize;
        if (resilience.MinConnectionPoolSize.HasValue)
        {
            settings.MinConnectionPoolSize = resilience.MinConnectionPoolSize.Value;
        }
        settings.RetryReads = resilience.RetryReads;
        settings.RetryWrites = resilience.RetryWrites;
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Registers user-defined convention packs for BSON serialization.
    ///     Only the conventions explicitly added via <see cref="MongoConventionOptions.AddConvention" /> will be registered —
    ///     the library's built-in defaults will NOT be applied.
    ///     This method is idempotent — only the first invocation takes effect.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     注册用户自定义的 BSON 序列化约定包。
    ///     仅注册通过 <see cref="MongoConventionOptions.AddConvention" /> 显式添加的约定 — 本库的内置默认约定不会被应用。
    ///     此方法是幂等的 — 仅首次调用生效。
    ///     </para>
    /// </summary>
    /// <param name="options">The convention options. Must not be null.</param>
    internal static void RegistryConventionPack(MongoConventionOptions options)
    {
        if (Interlocked.CompareExchange(ref _conventionsRegistered, 1, 0) != 0)
        {
            return;
        }
        // 判断用户自定义 convention 中是否包含 CamelCase
        UseCamelCase = options.Conventions.Any(e => e.Pack.Conventions.Any(c => c is CamelCaseElementNameConvention));
        foreach (var entry in options.Conventions)
        {
            ConventionRegistry.Register(entry.Name, entry.Pack, _ => true);
        }
        RegisterIdGeneratorAndSerializers(options.ObjectIdToStringTypes);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Ensures conventions are registered. If <c>ConfigureMongoConventions</c> was not called explicitly,
    ///     registers the library's built-in default conventions automatically.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     确保 Convention 已注册。若用户未显式调用 <c>ConfigureMongoConventions</c>，则自动注册本库的内置默认约定。
    ///     </para>
    /// </summary>
    internal static void EnsureConventionsRegistered()
    {
        if (Interlocked.CompareExchange(ref _conventionsRegistered, 1, 0) != 0)
        {
            return;
        }
        UseCamelCase = true;
        ConventionRegistry.Register($"{Constant.Pack}-default", new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true),
            new NamedIdMemberConvention("Id", "ID"),
            new EnumRepresentationConvention(BsonType.String)
        }, _ => true);
        RegisterIdGeneratorAndSerializers([]);
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Registers the ObjectId-to-string ID generator convention and global serializers.
    ///     Shared by both user-defined and default convention registration paths.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     注册 ObjectId 到字符串的 ID 生成器约定和全局序列化器。
    ///     用户自定义和默认约定注册路径共用此方法。
    ///     </para>
    /// </summary>
    private static void RegisterIdGeneratorAndSerializers(List<Type> objectIdToStringTypes)
    {
        ConventionRegistry.Register($"{Constant.Pack}-id-generator", new ConventionPack
        {
            new StringToObjectIdIdGeneratorConvention() //ObjectId → String mapping ObjectId
        }, x => !objectIdToStringTypes.Contains(x));
        // 确保全局序列化器只注册一次
        _ = MongoServiceExtensions.FirstInitialization.Value;
    }
}