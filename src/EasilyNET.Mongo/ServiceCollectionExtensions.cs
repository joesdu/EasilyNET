using EasilyNET.Mongo.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Mongo;

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
    /// 是否是第一次注册BsonSerializer
    /// </summary>
    private static bool first;

    /// <summary>
    /// 通过默认连接字符串名称配置添加 <see cref="MongoContext" />
    /// </summary>
    /// <typeparam name="T"><see cref="MongoContext" />子类</typeparam>
    /// <param name="services"><see cref="IServiceCollection" /> Services</param>
    /// <param name="configuration"><see cref="IConfiguration" /> 配置</param>
    /// <param name="option"><see cref="BasicClientOptions" /> 其他一些配置</param>
    public static void AddMongoContext<T>(this IServiceCollection services, IConfiguration configuration, Action<ClientOptions>? option = null) where T : MongoContext
    {
        var connStr = configuration["CONNECTIONSTRINGS_MONGO"] ?? configuration.GetConnectionString("Mongo") ?? throw new("💔:no [CONNECTIONSTRINGS_MONGO] env or ConnectionStrings.Mongo is null in appsettings.json");
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
        var mongoUrl = new MongoUrl(connStr);
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
                new CamelCaseElementNameConvention(),             // 驼峰名称格式
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
            new StringObjectIdIdGeneratorConvention() //ObjectId → String mapping ObjectId
        }, x => !options.ObjectIdToStringTypes.Contains(x));
        if (first) return;
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Local)); //to local time
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128)); //decimal to decimal default
        first = !first;
    }
}