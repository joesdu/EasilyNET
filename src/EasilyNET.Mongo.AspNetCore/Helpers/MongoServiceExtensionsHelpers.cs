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
    /// registered conditionally according to the provided options. This method should be called once during application
    /// initialization to ensure consistent serialization behavior.
    /// </remarks>
    /// <param name="options">The client options that determine which convention packs are registered. Must not be null.</param>
    internal static void RegistryConventionPack(BasicClientOptions options)
    {
        if (options.DefaultConventionRegistry)
        {
            ConventionRegistry.Register($"{Constant.Pack}-{ObjectId.GenerateNewId()}", new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true),
                new NamedIdMemberConvention("Id", "ID"),
                new EnumRepresentationConvention(BsonType.String)
            }, _ => true);
        }
        foreach (var item in options.ConventionRegistry)
        {
            ConventionRegistry.Register(item.Key, item.Value, _ => true);
        }
        ConventionRegistry.Register($"easily-id-pack-{ObjectId.GenerateNewId()}", new ConventionPack
        {
            new StringToObjectIdIdGeneratorConvention() //ObjectId → String mapping ObjectId
        }, x => !options.ObjectIdToStringTypes.Contains(x));
        // 确保全局序列化器只注册一次
        _ = MongoServiceExtensions.FirstInitialization.Value;
    }
}