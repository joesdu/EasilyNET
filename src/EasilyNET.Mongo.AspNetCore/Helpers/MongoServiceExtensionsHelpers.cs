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
    /// This method configures global BSON serialization conventions for MongoDB entities, including
    /// element naming, extra element handling, ID member naming, and enum representation. Convention packs are
    /// registered conditionally according to the provided options. Default conventions and the Id generator
    /// convention are registered only once (thread-safe) to prevent memory leaks from duplicate registrations
    /// in the global <see cref="ConventionRegistry" />.
    /// </remarks>
    /// <param name="options">The client options that determine which convention packs are registered. Must not be null.</param>
    internal static void RegistryConventionPack(BasicClientOptions options)
    {
        // 默认 convention 和 Id 生成器 convention 仅注册一次，避免全局 ConventionRegistry 内存泄漏
        if (Interlocked.CompareExchange(ref _defaultConventionsRegistered, 1, 0) == 0)
        {
            _firstObjectIdToStringTypes = options.ObjectIdToStringTypes;
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
        // 用户自定义 convention 仍按需注册（使用用户提供的 key，由用户保证唯一性）
        foreach (var item in options.ConventionRegistry)
        {
            ConventionRegistry.Register(item.Key, item.Value, _ => true);
        }
    }
}