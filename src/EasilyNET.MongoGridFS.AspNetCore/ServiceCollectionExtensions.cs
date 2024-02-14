using EasilyNET.MongoGridFS.AspNetCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务扩展
/// </summary>
public static class ServiceCollectionExtensions
{
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
    /// 使用链接字符的方式配置MongoGridFS
    /// </summary>
    /// <param name="services"></param>
    /// <param name="connectionString">数据库链接字符串</param>
    /// <param name="configure"></param>
    /// <returns></returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, string connectionString, Action<GridFSBucketOptions>? configure = null)
    {
        var url = new MongoUrl(connectionString);
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
            c.DisableMD5 = true;
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
