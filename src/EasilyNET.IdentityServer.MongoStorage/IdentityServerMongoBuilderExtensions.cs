using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using EasilyNET.IdentityServer.MongoStorage;
using EasilyNET.IdentityServer.MongoStorage.Abstraction;
using EasilyNET.IdentityServer.MongoStorage.Configuration;
using EasilyNET.IdentityServer.MongoStorage.DbContexts;
using EasilyNET.IdentityServer.MongoStorage.Entities;
using EasilyNET.IdentityServer.MongoStorage.Options;
using EasilyNET.IdentityServer.MongoStorage.Services;
using EasilyNET.IdentityServer.MongoStorage.Stores;
using Microsoft.Extensions.Hosting;
using MongoDB.Bson.Serialization;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// IdentityServerMongoBuilderExtensions
/// </summary>
public static class IdentityServerMongoBuilderExtensions
{
    /// <summary>
    /// 添加配置存储
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddConfigurationStore(this IIdentityServerBuilder builder, Action<MongoDBConfiguration> configuration)
    {
        builder.Services.Configure(configuration);
        BsonClassMap.RegisterClassMap<Client>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<IdentityResource>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<ApiResource>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<ApiScope>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        builder.Services.AddScoped<IConfigurationDbContext, ConfigurationDbContext>();
        builder.Services.AddTransient<IClientStore, ClientStore>();
        builder.Services.AddTransient<IResourceStore, ResourceStore>();
        builder.Services.AddTransient<ICorsPolicyService, CorsPolicyService>();
        return builder;
    }

    /// <summary>
    /// 添加操作存储
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="tokenCleanUpOptions"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddOperationalStore(this IIdentityServerBuilder builder, Action<TokenCleanupOptions>? tokenCleanUpOptions = null)
    {
        BsonClassMap.RegisterClassMap<PersistedGrant>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        builder.Services.AddScoped<IPersistedGrantDbContext, PersistedGrantDbContext>();
        builder.Services.AddTransient<IPersistedGrantStore, PersistedGrantStore>();
        var tco = new TokenCleanupOptions();
        tokenCleanUpOptions?.Invoke(tco);
        builder.Services.AddSingleton(tco);
        builder.Services.AddTransient<TokenCleanup>();
        builder.Services.AddSingleton<IHostedService, TokenCleanupService>();
        return builder;
    }
}