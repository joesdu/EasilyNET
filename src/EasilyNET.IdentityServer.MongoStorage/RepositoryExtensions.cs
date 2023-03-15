using Duende.IdentityServer.Services;
using Duende.IdentityServer.Stores;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// 扩展
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// 添加Mongo持久化,请确保已经注册IMongoDatabase和IMongoClient
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="dbName">数据库名称</param>
    /// <param name="prefix">集合前缀,默认添加: easilynet.</param>
    /// <returns></returns>
    /// <exception cref="NullReferenceException"></exception>
    public static IIdentityServerBuilder AddMongoRepository(this IIdentityServerBuilder builder, string? dbName = null, string? prefix = "easilynet.")
    {
        var db = builder.Services.BuildServiceProvider().GetService<IMongoDatabase>() ?? throw new NullReferenceException("mongo database not found!");
        if (dbName is not null)
        {
            var client = builder.Services.BuildServiceProvider().GetService<IMongoClient>() ?? throw new NullReferenceException("mongo client not found!");
            db = client.GetDatabase(dbName);
        }
        _ = builder.Services.AddTransient<IRepository, MongoRepository>(_ => new(db, prefix));
        return builder;
    }

    /// <summary>
    /// 添加身份客户端
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddIdentityClients(this IIdentityServerBuilder builder)
    {
        _ = builder.Services.AddTransient<IClientStore, RepositoryClientStore>();
        return builder;
    }

    /// <summary>
    /// 添加身份资源
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddIdentityResources(this IIdentityServerBuilder builder)
    {
        _ = builder.Services.AddTransient<IResourceStore, RepositoryResourceStore>();
        return builder;
    }

    /// <summary>
    /// 添加身份授权
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddIdentityPersistedGrants(this IIdentityServerBuilder builder)
    {
        _ = builder.Services.AddTransient<IPersistedGrantStore, RepositoryPersistedGrantStore>();
        return builder;
    }

    /// <summary>
    /// 添加策略服务
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IIdentityServerBuilder AddPolicyService(this IIdentityServerBuilder builder)
    {
        _ = builder.Services.AddSingleton<ICorsPolicyService, RepositoryCorsPolicyService>();
        return builder;
    }
}