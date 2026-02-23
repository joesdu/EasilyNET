using System.Collections.Concurrent;
using System.Diagnostics;
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
    ///     Thread-safe flag to ensure default convention packs and the Id generator convention are registered only once,
    ///     preventing unbounded accumulation in the global <see cref="ConventionRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh">线程安全标志，确保默认 convention pack 和 Id 生成器 convention 仅注册一次，防止在全局 ConventionRegistry 中无限累积。</para>
    /// </summary>
    private static int _defaultConventionsRegistered;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Stores the ObjectIdToStringTypes from the first registration so the Id generator convention filter remains consistent.
    ///     </para>
    ///     <para xml:lang="zh">存储首次注册时的 ObjectIdToStringTypes，以确保 Id 生成器 convention 过滤器保持一致。</para>
    /// </summary>
    private static List<Type>? _firstObjectIdToStringTypes;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Stores the DefaultConventionRegistry value from the first registration for consistency detection.
    ///     </para>
    ///     <para xml:lang="zh">存储首次注册时的 DefaultConventionRegistry 值，用于一致性检测。</para>
    /// </summary>
    private static bool _firstDefaultConventionRegistry;

    /// <summary>
    ///     <para xml:lang="en">Tracks user custom convention keys that have been registered globally.</para>
    ///     <para xml:lang="zh">跟踪已全局注册的用户自定义 convention key，避免重复注册。</para>
    /// </summary>
    private static readonly ConcurrentDictionary<string, byte> UserConventionKeys = new(StringComparer.Ordinal);

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
    /// Registers convention packs for BSON serialization based on the specified client options.
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     This method configures global BSON serialization conventions for MongoDB entities, including
    ///     element naming, extra element handling, ID member naming, and enum representation. Convention packs are
    ///     registered conditionally according to the provided options. Default conventions and the Id generator
    ///     convention are registered only once (thread-safe) to prevent memory leaks from duplicate registrations
    ///     in the global <see cref="ConventionRegistry" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此方法为 MongoDB 实体配置全局 BSON 序列化约定，包括元素命名、额外元素处理、ID 成员命名和枚举表示。
    ///     约定包根据提供的选项有条件地注册。默认约定和 Id 生成器约定仅注册一次（线程安全），以防止在全局
    ///     <see cref="ConventionRegistry" /> 中因重复注册导致内存泄漏。
    ///     </para>
    ///     <para xml:lang="en">
    ///     <b>Known limitation:</b> Because <see cref="ConventionRegistry" /> is a process-wide global registry,
    ///     the <see cref="BasicClientOptions.DefaultConventionRegistry" /> and <see cref="BasicClientOptions.ObjectIdToStringTypes" />
    ///     settings from the <b>first</b> <c>AddMongoContext</c> call take effect globally. Subsequent registrations with
    ///     different settings will be silently ignored. If you need different conventions for different contexts,
    ///     use <see cref="BasicClientOptions.ConventionRegistry" /> to register custom convention packs instead.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     <b>已知限制：</b>由于 <see cref="ConventionRegistry" /> 是进程级全局注册表，
    ///     <see cref="BasicClientOptions.DefaultConventionRegistry" /> 和 <see cref="BasicClientOptions.ObjectIdToStringTypes" />
    ///     仅以<b>首次</b> <c>AddMongoContext</c> 调用时的设置为准，后续注册若使用不同设置将被静默忽略。
    ///     如需为不同的 Context 配置不同的约定，请使用 <see cref="BasicClientOptions.ConventionRegistry" /> 注册自定义约定包。
    ///     </para>
    /// </remarks>
    /// <param name="options">The client options that determine which convention packs are registered. Must not be null.</param>
    internal static void RegistryConventionPack(BasicClientOptions options)
    {
        // 默认 convention 和 Id 生成器 convention 仅注册一次，避免全局 ConventionRegistry 内存泄漏
        if (Interlocked.CompareExchange(ref _defaultConventionsRegistered, 1, 0) == 0)
        {
            _firstObjectIdToStringTypes = options.ObjectIdToStringTypes;
            _firstDefaultConventionRegistry = options.DefaultConventionRegistry;
            if (options.DefaultConventionRegistry)
            {
                ConventionRegistry.Register($"{Constant.Pack}-default", new ConventionPack
                {
                    new CamelCaseElementNameConvention(),
                    new IgnoreExtraElementsConvention(true),
                    new NamedIdMemberConvention("Id", "ID"),
                    new EnumRepresentationConvention(BsonType.String)
                }, _ => true);
            }
            ConventionRegistry.Register($"{Constant.Pack}-id-generator", new ConventionPack
            {
                new StringToObjectIdIdGeneratorConvention() //ObjectId → String mapping ObjectId
            }, x => !(_firstObjectIdToStringTypes ?? []).Contains(x));
            // 确保全局序列化器只注册一次
            _ = MongoServiceExtensions.FirstInitialization.Value;
        }
        else
        {
            // 检测后续注册与首次注册的设置是否一致，输出诊断警告
            if (options.DefaultConventionRegistry != _firstDefaultConventionRegistry)
            {
                Trace.TraceWarning("[EasilyNET.Mongo] 当前 AddMongoContext 调用的 DefaultConventionRegistry={0} 与首次注册的值 {1} 不一致，该设置已被忽略。ConventionRegistry 是进程级全局注册表，仅首次注册生效。",
                    options.DefaultConventionRegistry, _firstDefaultConventionRegistry);
            }
            var currentTypes = options.ObjectIdToStringTypes;
            var firstTypes = _firstObjectIdToStringTypes ?? [];
            if (!currentTypes.SequenceEqual(firstTypes))
            {
                Trace.TraceWarning("[EasilyNET.Mongo] 当前 AddMongoContext 调用的 ObjectIdToStringTypes 与首次注册不一致，该设置已被忽略。仅首次注册的 ObjectIdToStringTypes 生效。");
            }
        }
        // 用户自定义 convention 去重注册，避免 ConventionRegistry 全局累积
        foreach (var item in options.ConventionRegistry)
        {
            if (!UserConventionKeys.TryAdd(item.Key, 0))
            {
                Trace.TraceWarning("[EasilyNET.Mongo] 自定义 Convention key '{0}' 已注册，已跳过重复注册。", item.Key);
                continue;
            }
            ConventionRegistry.Register(item.Key, item.Value, _ => true);
        }
    }
}