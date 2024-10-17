using EasilyNET.Mongo.AspNetCore.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore;

/// <summary>
/// MongoGridFS扩展
/// </summary>
public static class GridFSCollectionExtensions
{
    /// <summary>
    /// 使用容器中的 <see cref="IMongoDatabase" /> 来配置MongoGridFS
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, Action<GridFSBucketOptions>? configure = null)
    {
        var db = services.BuildServiceProvider().GetService<IMongoDatabase>() ?? throw new("请先注册IMongoDatabase服务");
        services.AddMongoGridFS(db, configure);
        return services;
    }

    /// <summary>
    /// 使用 <see cref="MongoClientSettings" /> 来配置MongoGridFS
    /// </summary>
    /// <param name="services"></param>
    /// <param name="mongoSettings"></param>
    /// <param name="dbName">数据库名称</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, MongoClientSettings mongoSettings, string? dbName = null, Action<GridFSBucketOptions>? configure = null)
    {
        var db = new MongoClient(mongoSettings).GetDatabase(dbName ?? Constant.DefaultDbName);
        services.AddMongoGridFS(db, configure);
        return services;
    }

    /// <summary>
    /// 使用 <see cref="IConfiguration" /> 配置MongoGridFS
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration">从环境变量和appsettings.json中读取,若是appsettings.json中不存在则会回退到环境变量中读取</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, IConfiguration configuration, Action<GridFSBucketOptions>? configure = null)
    {
        var connStr = configuration.GetConnectionString("Mongo") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_MONGO");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new("💔: appsettings.json中无ConnectionStrings.Mongo配置或环境变量中不存在CONNECTIONSTRINGS_MONGO");
        }
        var url = MongoUrl.Create(connStr);
        var name = string.IsNullOrWhiteSpace(url.DatabaseName) ? Constant.DefaultDbName : url.DatabaseName;
        var db = new MongoClient(url).GetDatabase(name);
        services.AddMongoGridFS(db, configure);
        return services;
    }

    /// <summary>
    /// 使用已有的 IMongoDatabase
    /// </summary>
    /// <param name="services"></param>
    /// <param name="db"></param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, IMongoDatabase db, Action<GridFSBucketOptions>? configure = null)
    {
        services.AddMongoGridFS(db, Constant.ConfigName, c =>
        {
            c.BucketName = Constant.BucketName;
            c.ChunkSizeBytes = 1024;
            c.ReadConcern = new();
            c.ReadPreference = ReadPreference.Primary;
            c.WriteConcern = WriteConcern.Unacknowledged;
            configure?.Invoke(c);
        });
        return services;
    }

    /// <summary>
    /// 通过 <see cref="IMongoDatabase" /> 注册 <see cref="IGridFSBucket" />
    /// </summary>
    /// <param name="services"></param>
    /// <param name="db"></param>
    /// <param name="name">ConfigureName</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, IMongoDatabase db, string name, Action<GridFSBucketOptions> configure)
    {
        services.Configure(name, configure);
        services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(db));
        return services;
    }
}