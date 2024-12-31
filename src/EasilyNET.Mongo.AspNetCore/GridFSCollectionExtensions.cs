using EasilyNET.Mongo.AspNetCore;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">MongoGridFS extensions</para>
///     <para xml:lang="zh">MongoGridFS扩展</para>
/// </summary>
public static class GridFSCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Configure MongoGridFS using <see cref="IMongoDatabase" /> from the container</para>
    ///     <para xml:lang="zh">使用容器中的 <see cref="IMongoDatabase" /> 来配置MongoGridFS</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, Action<GridFSBucketOptions>? configure = null)
    {
        var db = services.BuildServiceProvider().GetService<IMongoDatabase>() ?? throw new("请先注册IMongoDatabase服务");
        services.AddMongoGridFS(db, configure);
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure MongoGridFS using <see cref="MongoClientSettings" /></para>
    ///     <para xml:lang="zh">使用 <see cref="MongoClientSettings" /> 来配置MongoGridFS</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="mongoSettings">
    ///     <para xml:lang="en">Mongo client settings</para>
    ///     <para xml:lang="zh">Mongo客户端设置</para>
    /// </param>
    /// <param name="dbName">
    ///     <para xml:lang="en">Database name</para>
    ///     <para xml:lang="zh">数据库名称</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, MongoClientSettings mongoSettings, string? dbName = null, Action<GridFSBucketOptions>? configure = null)
    {
        var db = new MongoClient(mongoSettings).GetDatabase(dbName ?? Constant.DefaultDbName);
        services.AddMongoGridFS(db, configure);
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Configure MongoGridFS using <see cref="IConfiguration" /></para>
    ///     <para xml:lang="zh">使用 <see cref="IConfiguration" /> 配置MongoGridFS</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="configuration">
    ///     <para xml:lang="en">
    ///     Configuration from environment variables and appsettings.json. If not found in appsettings.json, it will fall back to
    ///     environment variables.
    ///     </para>
    ///     <para xml:lang="zh">从环境变量和appsettings.json中读取,若是appsettings.json中不存在则会回退到环境变量中读取</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
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
    ///     <para xml:lang="en">Configure MongoGridFS using an existing <see cref="IMongoDatabase" /></para>
    ///     <para xml:lang="zh">使用已有的 <see cref="IMongoDatabase" /> 配置MongoGridFS</para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="db">
    ///     <para xml:lang="en">Mongo database</para>
    ///     <para xml:lang="zh">Mongo数据库</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
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
    ///     <para xml:lang="en">Register <see cref="IGridFSBucket" /> through <see cref="IMongoDatabase" /></para>
    ///     <para xml:lang="zh">通过 <see cref="IMongoDatabase" /> 注册 <see cref="IGridFSBucket" /></para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    /// <param name="db">
    ///     <para xml:lang="en">Mongo database</para>
    ///     <para xml:lang="zh">Mongo数据库</para>
    /// </param>
    /// <param name="name">
    ///     <para xml:lang="en">Configuration name</para>
    ///     <para xml:lang="zh">配置名称</para>
    /// </param>
    /// <param name="configure">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </returns>
    public static IServiceCollection AddMongoGridFS(this IServiceCollection services, IMongoDatabase db, string name, Action<GridFSBucketOptions> configure)
    {
        services.Configure(name, configure);
        services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
        services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(db));
        return services;
    }
}