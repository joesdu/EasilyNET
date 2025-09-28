using EasilyNET.Core.Misc;
using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Conventions;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Service extension class</para>
///     <para xml:lang="zh">服务扩展类</para>
///     <description>
///         <para xml:lang="en">
///         Create a DbContext use connectionString with [ConnectionStrings.Mongo in appsettings.json] or with [CONNECTIONSTRINGS_MONGO] setting
///         value in environment variable
///         </para>
///         <para xml:lang="zh">
///         使用 appsettings.json 中的 [ConnectionStrings.Mongo] 或环境变量中的 [CONNECTIONSTRINGS_MONGO] 设置值创建 DbContext
///         </para>
///     </description>
/// </summary>
public static class MongoServiceExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Lazy&lt;T&gt; provides a thread-safe lazy initialization mechanism to ensure that the global serializer registration logic is
    ///     executed only once. This variable is used to register global DateTime and Decimal serializers on first access.
    ///     </para>
    ///     <para xml:lang="zh">Lazy&lt;T&gt; 提供了线程安全的延迟初始化机制，确保全局序列化器的注册逻辑只执行一次。该变量用于在第一次访问时注册全局的 DateTime 和 Decimal 序列化器。</para>
    /// </summary>
    private static readonly Lazy<bool> FirstInitialization = new(() =>
    {
        // 注册全局 DateTime 序列化器，将 DateTime 序列化为本地时间
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Local));
        // 注册全局 Decimal 序列化器，将 decimal 序列化为 Decimal128
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
        return true;
    });

    private static void RegistryConventionPack(BasicClientOptions options)
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
        _ = FirstInitialization.Value;
    }

    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">Add <see cref="MongoContext" /> through the default connection string name</para>
        ///     <para xml:lang="zh">通过默认连接字符串名称配置添加 <see cref="MongoContext" /></para>
        /// </summary>
        /// <typeparam name="T">
        ///     <see cref="MongoContext" />
        /// </typeparam>
        /// <param name="configuration">
        ///     <see cref="IConfiguration" />
        /// </param>
        /// <param name="option">
        ///     <see cref="BasicClientOptions" />
        /// </param>
        public void AddMongoContext<T>(IConfiguration configuration, Action<ClientOptions>? option = null) where T : MongoContext
        {
            var connStr = configuration.GetConnectionString("Mongo") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_MONGO");
            if (string.IsNullOrWhiteSpace(connStr))
            {
                throw new("💔: appsettings.json中无ConnectionStrings.Mongo配置或环境变量中不存在CONNECTIONSTRINGS_MONGO");
            }
            services.AddMongoContext<T>(connStr, option);
        }

        /// <summary>
        ///     <para xml:lang="en">Add <see cref="MongoContext" /> through the connection string</para>
        ///     <para xml:lang="zh">通过连接字符串配置添加 <see cref="MongoContext" /></para>
        /// </summary>
        /// <typeparam name="T">
        ///     <see cref="MongoContext" />
        /// </typeparam>
        /// <param name="connStr">
        ///     <para xml:lang="en"><see langword="string" /> MongoDB connection string</para>
        ///     <para xml:lang="zh"><see langword="string" /> MongoDB链接字符串</para>
        /// </param>
        /// <param name="option">
        ///     <see cref="BasicClientOptions" />
        /// </param>
        public void AddMongoContext<T>(string connStr, Action<ClientOptions>? option = null) where T : MongoContext
        {
            // 从字符串解析Url
            var mongoUrl = MongoUrl.Create(connStr);
            var settings = MongoClientSettings.FromUrl(mongoUrl);
            // 配置自定义配置
            var options = new ClientOptions();
            option?.Invoke(options);
            options.ClientSettings?.Invoke(settings);
            var dbName = !string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? mongoUrl.DatabaseName : options.DatabaseName ?? Constant.DefaultDbName;
            if (options.DatabaseName is not null)
            {
                dbName = options.DatabaseName;
            }
            services.AddMongoContext<T>(settings, c =>
            {
                c.ObjectIdToStringTypes = options.ObjectIdToStringTypes;
                c.DefaultConventionRegistry = options.DefaultConventionRegistry;
                c.ConventionRegistry.AddRange(options.ConventionRegistry);
                c.DatabaseName = dbName;
            });
        }

        /// <summary>
        ///     <para xml:lang="en">Add <see cref="MongoContext" /> using <see cref="MongoClientSettings" /> configuration</para>
        ///     <para xml:lang="zh">使用 <see cref="MongoClientSettings" /> 配置添加 <see cref="MongoContext" /></para>
        /// </summary>
        /// <typeparam name="T">
        ///     <see cref="MongoContext" />
        /// </typeparam>
        /// <param name="settings">
        ///     <para xml:lang="en"><see cref="MongoClientSettings" /> MongoDB client settings</para>
        ///     <para xml:lang="zh"><see cref="MongoClientSettings" /> MongoDB客户端配置</para>
        /// </param>
        /// <param name="option">
        ///     <see cref="BasicClientOptions" />
        /// </param>
        public void AddMongoContext<T>(MongoClientSettings settings, Action<BasicClientOptions>? option = null) where T : MongoContext
        {
            var options = new BasicClientOptions();
            option?.Invoke(options);
            RegistryConventionPack(options);
            settings.MinConnectionPoolSize = Environment.ProcessorCount;
            var context = MongoContext.CreateInstance<T>(settings, options.DatabaseName ?? Constant.DefaultDbName);
            services.AddSingleton(context.Client);
            services.AddSingleton(context.Database);
            services.AddSingleton(context);
            services.AddSingleton(options);
        }
    }
}