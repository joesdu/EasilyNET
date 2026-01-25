using EasilyNET.Mongo.AspNetCore.Abstraction;
using EasilyNET.Mongo.AspNetCore.BackgroundServices;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Conventions;
using EasilyNET.Mongo.AspNetCore.Factories;
using EasilyNET.Mongo.AspNetCore.Helpers;
using EasilyNET.Mongo.AspNetCore.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
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
    /// <param name="services">
    ///     <para xml:lang="en">Service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using <see cref="IMongoDatabase" /> from the container</para>
        ///     <para xml:lang="zh">使用容器中的 <see cref="IMongoDatabase" /> 来配置MongoGridFS</para>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">Configuration action</para>
        ///     <para xml:lang="zh">配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(Action<GridFSBucketOptions>? configure = null, Action<GridFSServerOptions>? serverConfigure = null)
        {
            services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(sp.GetRequiredService<IMongoDatabase>()));
            services.TryAddSingleton<GridFSCleanupHelper>();
            services.AddHostedService<GridFSBackgroundCleanupService>();
            services.AddOptions<UploadValidationOptions>();
            services.TryAddSingleton<IUploadValidator, DefaultUploadValidator>();
            services.AddSingleton<GridFSHelper>(sp =>
            {
                var bucket = sp.GetRequiredService<IGridFSBucket>();
                var validator = sp.GetRequiredService<IUploadValidator>();
                var logger = sp.GetRequiredService<ILogger<GridFSHelper>>();
                return new(bucket, validator, logger);
            });
            services.AddSingleton<IGridFSUploadService>(sp => sp.GetRequiredService<GridFSHelper>());
            services.Configure<MvcOptions>(c => c.Conventions.Add(new GridFSControllerConvention(new())));
            services.Configure<FormOptions>(c =>
                    {
                        c.MultipartHeadersLengthLimit = int.MaxValue;
                        c.MultipartBodyLengthLimit = long.MaxValue;
                        c.ValueLengthLimit = int.MaxValue;
                    })
                    .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
                    .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
            services.AddOptions<GridFSBucketOptions>(Constant.ConfigName).Configure(options =>
            {
                options.BucketName = Constant.BucketName;
                options.ChunkSizeBytes = GridFSDefaults.StreamingChunkSize;
                options.ReadConcern = new();
                options.ReadPreference = ReadPreference.Primary;
                options.WriteConcern = WriteConcern.Unacknowledged;
                configure?.Invoke(options);
            });
            services.AddOptions<GridFSServerOptions>(Constant.ConfigName).Configure(options => { serverConfigure?.Invoke(options); });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS with upload validation options</para>
        ///     <para xml:lang="zh">配置 MongoGridFS 上传验证选项</para>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">Bucket configuration action</para>
        ///     <para xml:lang="zh">存储桶配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        /// <param name="validationConfigure">
        ///     <para xml:lang="en">Upload validation configuration</para>
        ///     <para xml:lang="zh">上传验证配置</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(Action<GridFSBucketOptions>? configure, Action<GridFSServerOptions>? serverConfigure, Action<UploadValidationOptions> validationConfigure)
        {
            services.Configure(validationConfigure);
            return services.AddMongoGridFS(configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using <see cref="MongoClientSettings" /></para>
        ///     <para xml:lang="zh">使用 <see cref="MongoClientSettings" /> 来配置MongoGridFS</para>
        /// </summary>
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
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(MongoClientSettings mongoSettings, string? dbName = null, Action<GridFSBucketOptions>? configure = null, Action<GridFSServerOptions>? serverConfigure = null)
        {
            services.TryAddSingleton<IMongoClient>(_ => new MongoClient(mongoSettings));
            services.TryAddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(dbName ?? Constant.DefaultDbName));
            return services.AddMongoGridFS(configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using <see cref="MongoClientSettings" /> with validation options</para>
        ///     <para xml:lang="zh">使用 <see cref="MongoClientSettings" /> 配置 MongoGridFS 并配置验证选项</para>
        /// </summary>
        /// <param name="mongoSettings">
        ///     <para xml:lang="en">Mongo client settings</para>
        ///     <para xml:lang="zh">Mongo客户端设置</para>
        /// </param>
        /// <param name="dbName">
        ///     <para xml:lang="en">Database name</para>
        ///     <para xml:lang="zh">数据库名称</para>
        /// </param>
        /// <param name="configure">
        ///     <para xml:lang="en">Bucket configuration action</para>
        ///     <para xml:lang="zh">存储桶配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        /// <param name="validationConfigure">
        ///     <para xml:lang="en">Upload validation configuration</para>
        ///     <para xml:lang="zh">上传验证配置</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(MongoClientSettings mongoSettings, string? dbName, Action<GridFSBucketOptions>? configure, Action<GridFSServerOptions>? serverConfigure, Action<UploadValidationOptions> validationConfigure)
        {
            services.Configure(validationConfigure);
            return services.AddMongoGridFS(mongoSettings, dbName, configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using <see cref="IConfiguration" /></para>
        ///     <para xml:lang="zh">使用 <see cref="IConfiguration" /> 配置MongoGridFS</para>
        /// </summary>
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
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IConfiguration configuration, Action<GridFSBucketOptions>? configure = null, Action<GridFSServerOptions>? serverConfigure = null)
        {
            var connStr = configuration.GetConnectionString("Mongo") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_MONGO");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new("💔: appsettings.json中无ConnectionStrings.Mongo配置或环境变量中不存在CONNECTIONSTRINGS_MONGO");
            }
            var url = MongoUrl.Create(connStr);
            var name = string.IsNullOrWhiteSpace(url.DatabaseName) ? Constant.DefaultDbName : url.DatabaseName;
            services.TryAddSingleton<IMongoClient>(_ => new MongoClient(url));
            services.TryAddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(name));
            return services.AddMongoGridFS(configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using <see cref="IConfiguration" /> with validation options</para>
        ///     <para xml:lang="zh">使用 <see cref="IConfiguration" /> 配置 MongoGridFS 并配置验证选项</para>
        /// </summary>
        /// <param name="configuration">
        ///     <para xml:lang="en">Configuration from environment variables and appsettings.json</para>
        ///     <para xml:lang="zh">从环境变量和 appsettings.json 读取配置</para>
        /// </param>
        /// <param name="configure">
        ///     <para xml:lang="en">Bucket configuration action</para>
        ///     <para xml:lang="zh">存储桶配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        /// <param name="validationConfigure">
        ///     <para xml:lang="en">Upload validation configuration</para>
        ///     <para xml:lang="zh">上传验证配置</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IConfiguration configuration, Action<GridFSBucketOptions>? configure, Action<GridFSServerOptions>? serverConfigure, Action<UploadValidationOptions> validationConfigure)
        {
            var connStr = configuration.GetConnectionString("Mongo") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_MONGO");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new("💔: appsettings.json中无ConnectionStrings.Mongo配置或环境变量中不存在CONNECTIONSTRINGS_MONGO");
            }
            services.Configure(validationConfigure);
            return services.AddMongoGridFS(configuration, configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using an existing <see cref="IMongoDatabase" /></para>
        ///     <para xml:lang="zh">使用已有的 <see cref="IMongoDatabase" /> 配置MongoGridFS</para>
        /// </summary>
        /// <param name="db">
        ///     <para xml:lang="en">Mongo database</para>
        ///     <para xml:lang="zh">Mongo数据库</para>
        /// </param>
        /// <param name="configure">
        ///     <para xml:lang="en">Configuration action</para>
        ///     <para xml:lang="zh">配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IMongoDatabase db, Action<GridFSBucketOptions>? configure = null, Action<GridFSServerOptions>? serverConfigure = null)
        {
            services.AddOptions<GridFSBucketOptions>(Constant.ConfigName).Configure(options =>
            {
                options.BucketName = Constant.BucketName;
                options.ChunkSizeBytes = GridFSDefaults.StreamingChunkSize; // 255KB - 优化流式传输性能
                options.ReadConcern = new();
                options.ReadPreference = ReadPreference.Primary;
                options.WriteConcern = WriteConcern.Unacknowledged;
                configure?.Invoke(options);
            });
            services.AddOptions<GridFSServerOptions>(Constant.ConfigName).Configure(options => serverConfigure?.Invoke(options));
            services.TryAddSingleton<GridFSServerOptions>(sp => sp.GetRequiredService<IOptionsSnapshot<GridFSServerOptions>>().Get(Constant.ConfigName));
            services.Configure<MvcOptions, GridFSServerOptions>((c, serverOptions) =>
            {
                c.Conventions.Add(new GridFSControllerConvention(serverOptions));
            });
            services.Configure<FormOptions>(c =>
                    {
                        c.MultipartHeadersLengthLimit = int.MaxValue;
                        c.MultipartBodyLengthLimit = long.MaxValue;
                        c.ValueLengthLimit = int.MaxValue;
                    })
                    .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
                    .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
            services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(db));
            services.TryAddSingleton<GridFSCleanupHelper>();
            services.AddHostedService<GridFSBackgroundCleanupService>();
            services.AddOptions<UploadValidationOptions>();
            services.TryAddSingleton<IUploadValidator, DefaultUploadValidator>();
            services.AddSingleton<GridFSHelper>(sp =>
            {
                var bucket = sp.GetRequiredService<IGridFSBucket>();
                var validator = sp.GetRequiredService<IUploadValidator>();
                var logger = sp.GetRequiredService<ILogger<GridFSHelper>>();
                return new(bucket, validator, logger);
            });
            services.AddSingleton<IGridFSUploadService>(sp => sp.GetRequiredService<GridFSHelper>());
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">Configure MongoGridFS using an existing <see cref="IMongoDatabase" /> with validation options</para>
        ///     <para xml:lang="zh">使用已有的 <see cref="IMongoDatabase" /> 配置MongoGridFS并配置验证选项</para>
        /// </summary>
        /// <param name="db">
        ///     <para xml:lang="en">Mongo database</para>
        ///     <para xml:lang="zh">Mongo数据库</para>
        /// </param>
        /// <param name="configure">
        ///     <para xml:lang="en">Configuration action</para>
        ///     <para xml:lang="zh">配置操作</para>
        /// </param>
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        /// <param name="validationConfigure">
        ///     <para xml:lang="en">Upload validation configuration</para>
        ///     <para xml:lang="zh">上传验证配置</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IMongoDatabase db, Action<GridFSBucketOptions>? configure, Action<GridFSServerOptions>? serverConfigure, Action<UploadValidationOptions> validationConfigure)
        {
            services.Configure(validationConfigure);
            return services.AddMongoGridFS(db, configure, serverConfigure);
        }

        /// <summary>
        ///     <para xml:lang="en">Register <see cref="IGridFSBucket" /> through <see cref="IMongoDatabase" /></para>
        ///     <para xml:lang="zh">通过 <see cref="IMongoDatabase" /> 注册 <see cref="IGridFSBucket" /></para>
        /// </summary>
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
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IMongoDatabase db, string name, Action<GridFSBucketOptions> configure, Action<GridFSServerOptions>? serverConfigure = null)
        {
            services.Configure(name, configure);
            var serverOptions = new GridFSServerOptions();
            serverConfigure?.Invoke(serverOptions);
            services.Configure<MvcOptions>(c => c.Conventions.Add(new GridFSControllerConvention(serverOptions)));
            services.Configure<FormOptions>(c =>
                    {
                        c.MultipartHeadersLengthLimit = int.MaxValue;
                        c.MultipartBodyLengthLimit = long.MaxValue;
                        c.ValueLengthLimit = int.MaxValue;
                    })
                    .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
                    .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
            services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(db));
            services.TryAddSingleton<GridFSCleanupHelper>();
            services.AddHostedService<GridFSBackgroundCleanupService>();
            services.AddOptions<UploadValidationOptions>();
            services.TryAddSingleton<IUploadValidator, DefaultUploadValidator>();
            services.AddSingleton<GridFSHelper>(sp =>
            {
                var bucket = sp.GetRequiredService<IGridFSBucket>();
                var validator = sp.GetRequiredService<IUploadValidator>();
                var logger = sp.GetRequiredService<ILogger<GridFSHelper>>();
                return new(bucket, validator, logger);
            });
            services.AddSingleton<IGridFSUploadService>(sp => sp.GetRequiredService<GridFSHelper>());
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">Register <see cref="IGridFSBucket" /> and validation options</para>
        ///     <para xml:lang="zh">注册 <see cref="IGridFSBucket" /> 并配置验证选项</para>
        /// </summary>
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
        /// <param name="serverConfigure">
        ///     <para xml:lang="en">Server configuration action</para>
        ///     <para xml:lang="zh">服务端配置操作</para>
        /// </param>
        /// <param name="validationConfigure">
        ///     <para xml:lang="en">Upload validation configuration</para>
        ///     <para xml:lang="zh">上传验证配置</para>
        /// </param>
        public IServiceCollection AddMongoGridFS(IMongoDatabase db, string name, Action<GridFSBucketOptions> configure, Action<GridFSServerOptions>? serverConfigure, Action<UploadValidationOptions> validationConfigure)
        {
            services.Configure(name, configure);
            services.Configure(validationConfigure);
            var serverOptions = new GridFSServerOptions();
            serverConfigure?.Invoke(serverOptions);
            services.Configure<MvcOptions>(c => c.Conventions.Add(new GridFSControllerConvention(serverOptions)));
            services.Configure<FormOptions>(c =>
                    {
                        c.MultipartHeadersLengthLimit = int.MaxValue;
                        c.MultipartBodyLengthLimit = long.MaxValue;
                        c.ValueLengthLimit = int.MaxValue;
                    })
                    .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
                    .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
            services.TryAddSingleton<IGridFSBucketFactory, GridFSBucketFactory>();
            services.TryAddSingleton(sp => sp.GetRequiredService<IGridFSBucketFactory>().CreateBucket(db));
            services.TryAddSingleton<GridFSCleanupHelper>();
            services.AddHostedService<GridFSBackgroundCleanupService>();
            services.AddOptions<UploadValidationOptions>();
            services.TryAddSingleton<IUploadValidator, DefaultUploadValidator>();
            services.AddSingleton<GridFSHelper>(sp =>
            {
                var bucket = sp.GetRequiredService<IGridFSBucket>();
                var validator = sp.GetRequiredService<IUploadValidator>();
                var logger = sp.GetRequiredService<ILogger<GridFSHelper>>();
                return new(bucket, validator, logger);
            });
            services.AddSingleton<IGridFSUploadService>(sp => sp.GetRequiredService<GridFSHelper>());
            return services;
        }
    }
}