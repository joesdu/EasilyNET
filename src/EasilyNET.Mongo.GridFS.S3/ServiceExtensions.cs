using EasilyNET.Mongo.GridFS.S3;
using EasilyNET.Mongo.GridFS.S3.Abstraction;
using EasilyNET.Mongo.GridFS.S3.Encryption;
using EasilyNET.Mongo.GridFS.S3.Security;
using EasilyNET.Mongo.GridFS.S3.Versioning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务扩展
/// </summary>
public static class ServiceExtensions
{
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
    /// <param name="options">
    ///     <para xml:lang="en">Configuration action</para>
    ///     <para xml:lang="zh">配置操作</para>
    /// </param>
    public static IServiceCollection AddMongoS3(this IServiceCollection services, IMongoDatabase db, Action<GridFSBucketOptions> options)
    {
        var op = new GridFSBucketOptions();
        options.Invoke(op);
        services.TryAddSingleton<IGridFSBucket>(_ => new GridFSBucket(db, op));
        services.TryAddSingleton<IObjectStorage, GridFSObjectStorage>();

        // Register security and encryption services
        services.TryAddSingleton<S3IamPolicyManager>();
        services.TryAddSingleton<S3ObjectVersioningManager>();
        services.TryAddSingleton<S3ServerSideEncryptionManager>(sp =>
        {
            // Get master key from configuration or environment variable
            var configuration = sp.GetRequiredService<IConfiguration>();
            var masterKey = Environment.GetEnvironmentVariable("EASILYNET_MASTER_KEY") ?? configuration["EasilyNET:MasterKey"]; // 32 bytes for AES-256
            // Validate master key length (must be 32 bytes for AES-256)
            return string.IsNullOrWhiteSpace(masterKey) || masterKey.Length != 32
                       ? throw new InvalidOperationException("Master key must be exactly 32 characters (256 bits) for AES-256 encryption")
                       : new(masterKey);
        });
        return services;
    }

    /// <summary>
    ///     <para xml:lang="en">Add MongoDB-based S3 IAM Policy Manager</para>
    ///     <para xml:lang="zh">添加基于MongoDB的S3 IAM策略管理器</para>
    /// </summary>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    /// <param name="database">
    ///     <para xml:lang="en">MongoDB database instance</para>
    ///     <para xml:lang="zh">MongoDB数据库实例</para>
    /// </param>
    public static void AddMongoS3IamPolicyManager(this IServiceCollection services, IMongoDatabase database)
    {
        services.AddSingleton(new MongoS3IamPolicyManager(database));
    }

    /// <summary>
    ///     <para xml:lang="en">Add MongoDB-based S3 IAM Policy Manager using service provider</para>
    ///     <para xml:lang="zh">使用服务提供程序添加基于MongoDB的S3 IAM策略管理器</para>
    /// </summary>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    public static void AddMongoS3IamPolicyManager(this IServiceCollection services)
    {
        services.AddSingleton<MongoS3IamPolicyManager>(sp =>
        {
            var database = sp.GetRequiredService<IMongoDatabase>();
            return new(database);
        });
    }
}