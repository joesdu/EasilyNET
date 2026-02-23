using EasilyNET.Mongo.AspNetCore.Common;
using EasilyNET.Mongo.AspNetCore.Helpers;
using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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
    internal static readonly Lazy<bool> FirstInitialization = new(() =>
    {
        // 注册全局 DateTime 序列化器，将 DateTime 序列化为本地时间
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Local));
        // 注册全局 Decimal 序列化器，将 decimal 序列化为 Decimal128
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
        return true;
    });

    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Configure global MongoDB convention packs. This method should be called at most once, before any <c>AddMongoContext</c> call.
        ///     If not called, default conventions will be applied automatically on the first <c>AddMongoContext</c> call.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     配置全局 MongoDB Convention 约定包。此方法最多调用一次，且应在所有 <c>AddMongoContext</c> 调用之前。
        ///     若未调用，首次 <c>AddMongoContext</c> 时将自动使用默认配置。
        ///     </para>
        /// </summary>
        /// <param name="configure">
        ///     <para xml:lang="en">
        ///     Action to configure <see cref="MongoConventionOptions" />.
        ///     Only the conventions explicitly added via <see cref="MongoConventionOptions.AddConvention" /> will be registered.
        ///     The library's built-in defaults (camelCase, IgnoreExtraElements, etc.) will NOT be applied when this method is called.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     配置 <see cref="MongoConventionOptions" /> 的委托。
        ///     仅注册通过 <see cref="MongoConventionOptions.AddConvention" /> 显式添加的约定。
        ///     调用此方法后，本库的内置默认约定（驼峰命名、忽略未知字段等）将不会被应用。
        ///     </para>
        /// </param>
        /// <returns>
        ///     <see cref="IServiceCollection" />
        /// </returns>
        public IServiceCollection ConfigureMongoConventions(Action<MongoConventionOptions> configure)
        {
            var options = new MongoConventionOptions();
            configure(options);
            MongoServiceExtensionsHelpers.RegistryConventionPack(options);
            return services;
        }

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
        ///     <see cref="ClientOptions" />
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
        ///     <see cref="ClientOptions" />
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
                c.Resilience = options.Resilience;
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
            // 确保 Convention 已注册（若用户未显式调用 ConfigureMongoConventions，则使用默认配置）
            MongoServiceExtensionsHelpers.EnsureConventionsRegistered();
            MongoServiceExtensionsHelpers.ApplyResilienceOptions(settings, options.Resilience);
            var dbName = options.DatabaseName ?? Constant.DefaultDbName;
            // 使用工厂委托注册，支持 DI 构造函数注入
            // 优先通过 ActivatorUtilities 创建实例（支持有参构造函数），失败则回退到无参构造函数
            services.AddSingleton<T>(sp =>
            {
                T context;
                try
                {
                    context = ActivatorUtilities.CreateInstance<T>(sp);
                }
                catch (Exception ex)
                {
                    // DI 构造失败时记录诊断信息，便于排查问题
                    var logger = sp.GetService<ILoggerFactory>()?.CreateLogger(nameof(MongoServiceExtensions));
                    logger?.LogDebug(ex, "通过 DI 创建 {TypeName} 失败，回退到无参构造函数。", typeof(T).Name);
                    context = Activator.CreateInstance<T>();
                }
                context.Initialize(settings, dbName);
                return context;
            });
            services.AddSingleton(options);
        }
    }
}