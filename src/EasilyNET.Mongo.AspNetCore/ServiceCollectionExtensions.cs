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

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 服务扩展类
/// <list type="number">
///     <item>
///     Create a DbContext use connectionString with [ConnectionStrings.Mongo in appsettings.json] or with [CONNECTIONSTRINGS_MONGO] setting value
///     in environment variable
///     </item>
///     <item>Inject <see cref="MongoContext" /> use services.AddSingleton(db)</item>
///     <item>Inject <see cref="IMongoDatabase" /> use services.AddSingleton(db.Database)</item>
///     <item>Inject <see cref="IMongoClient" /> use services.AddSingleton(db.Client)</item>
///     <item>添加SkyAPM的诊断支持.在添加服务的时候填入 ClusterConfigurator,为减少依赖,所以需手动填入</item>
/// </list>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Lazy&lt;T&gt; 提供了线程安全的延迟初始化机制，确保全局序列化器的注册逻辑只执行一次。
    /// 该变量用于在第一次访问时注册全局的 DateTime 和 Decimal 序列化器。
    /// </summary>
    private static readonly Lazy<bool> _firstInitialization = new(() =>
    {
        // 注册全局 DateTime 序列化器，将 DateTime 序列化为本地时间
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Local));
        // 注册全局 Decimal 序列化器，将 decimal 序列化为 Decimal128
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128));
        return true;
    });

    /// <summary>
    /// 通过默认连接字符串名称配置添加 <see cref="MongoContext" />
    /// </summary>
    /// <typeparam name="T"><see cref="MongoContext" />子类</typeparam>
    /// <param name="services"><see cref="IServiceCollection" /> Services</param>
    /// <param name="configuration"><see cref="IConfiguration" /> 配置</param>
    /// <param name="option"><see cref="BasicClientOptions" /> 其他一些配置</param>
    public static void AddMongoContext<T>(this IServiceCollection services, IConfiguration configuration, Action<ClientOptions>? option = null) where T : MongoContext
    {
        var connStr = configuration.GetConnectionString("Mongo") ?? Environment.GetEnvironmentVariable("CONNECTIONSTRINGS_MONGO");
        if (string.IsNullOrWhiteSpace(connStr))
        {
            throw new("💔: appsettings.json中无ConnectionStrings.Mongo配置或环境变量中不存在CONNECTIONSTRINGS_MONGO");
        }
        services.AddMongoContext<T>(connStr, option);
    }

    /// <summary>
    /// 通过连接字符串配置添加 <see cref="MongoContext" />
    /// </summary>
    /// <typeparam name="T"><see cref="MongoContext" />子类</typeparam>
    /// <param name="services"><see cref="IServiceCollection" /> Services</param>
    /// <param name="connStr"><see langword="string" /> MongoDB链接字符串</param>
    /// <param name="option"><see cref="BasicClientOptions" /> 其他一些配置</param>
    public static void AddMongoContext<T>(this IServiceCollection services, string connStr, Action<ClientOptions>? option = null) where T : MongoContext
    {
        // 从字符串解析Url
        var mongoUrl = MongoUrl.Create(connStr);
        var settings = MongoClientSettings.FromUrl(mongoUrl);
        // 配置自定义配置
        var options = new ClientOptions();
        option?.Invoke(options);
        options.ClientSettings?.Invoke(settings);
        var dbName = !string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? mongoUrl.DatabaseName : options.DatabaseName ?? Constant.DbName;
        if (options.DatabaseName is not null) dbName = options.DatabaseName;
        services.AddMongoContext<T>(settings, c =>
        {
            c.ObjectIdToStringTypes = options.ObjectIdToStringTypes;
            c.DefaultConventionRegistry = options.DefaultConventionRegistry;
            c.ConventionRegistry = options.ConventionRegistry;
            c.DatabaseName = dbName;
        });
    }

    /// <summary>
    /// 使用 <see cref="MongoClientSettings" /> 配置添加 <see cref="MongoContext" />
    /// </summary>
    /// <typeparam name="T"><see cref="MongoContext" />子类</typeparam>
    /// <param name="services"><see cref="IServiceCollection" /> Services</param>
    /// <param name="settings"><see cref="MongoClientSettings" /> MongoDB客户端配置</param>
    /// <param name="option"><see cref="BasicClientOptions" /> 其他一些配置</param>
    public static void AddMongoContext<T>(this IServiceCollection services, MongoClientSettings settings, Action<BasicClientOptions>? option = null) where T : MongoContext
    {
        var options = new BasicClientOptions();
        option?.Invoke(options);
        RegistryConventionPack(options);
        settings.MinConnectionPoolSize = Environment.ProcessorCount;
        var db = MongoContext.CreateInstance<T>(settings, options.DatabaseName ?? Constant.DbName);
        services.AddSingleton(db).AddSingleton(db.Database).AddSingleton(db.Client);
    }

    private static void RegistryConventionPack(BasicClientOptions options)
    {
        if (options.DefaultConventionRegistry)
        {
            ConventionRegistry.Register($"{Constant.Pack}-{ObjectId.GenerateNewId()}", new ConventionPack
            {
                new CamelCaseElementNameConvention(),             // 小驼峰名称格式
                new IgnoreExtraElementsConvention(true),          // 忽略掉实体中不存在的字段
                new NamedIdMemberConvention("Id", "ID"),          // _id映射为实体中的ID或者Id
                new EnumRepresentationConvention(BsonType.String) // 将枚举类型存储为字符串格式
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
        _ = _firstInitialization.Value;
    }
}