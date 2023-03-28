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
/// 1.Create a DbContext use connectionString with [ConnectionStrings.Mongo in appsettings.json] or with
/// [CONNECTIONSTRINGS_MONGO] setting value in environment variable
/// 2.Inject DbContext use services.AddSingleton(db);
/// 3.Inject IMongoDataBase use services.AddSingleton(db._database);
/// 4.添加SkyAPM的诊断支持.在添加服务的时候填入 ClusterConfigurator,为减少依赖,所以需手动填入
/// </summary>
public static class MongoServiceExtensions
{
    /// <summary>
    /// 是否是第一次注册BsonSerializer
    /// </summary>
    private static bool first;

    /// <summary>
    /// 通过默认连接字符串名称添加DbContext
    /// </summary>
    /// <typeparam name="T">DbContext</typeparam>
    /// <param name="services">IServiceCollection</param>
    /// <param name="provider">IServiceProvider</param>
    /// <param name="configuration">IConfiguration</param>
    /// <param name="param">其他参数</param>
    /// <returns></returns>
    public static IServiceCollection AddMongoContext<T>(this IServiceCollection services, IServiceProvider provider, IConfiguration configuration, Action<EasilyNETMongoParams>? param = null)
        where T : EasilyNETMongoContext
    {
        var connStr = ConnectionString(configuration);
        _ = services.AddMongoContext<T>(provider, connStr, param);
        return services;
    }

    /// <summary>
    /// 使用MongoClientSettings配置添加DbContext
    /// </summary>
    /// <typeparam name="T">DbContext</typeparam>
    /// <param name="services">IServiceCollection</param>
    /// <param name="provider">IServiceProvider</param>
    /// <param name="settings">HoyoMongoClientSettings</param>
    /// <param name="param">其他参数</param>
    /// <returns></returns>
    public static IServiceCollection AddMongoContext<T>(this IServiceCollection services, IServiceProvider provider, MongoClientSettings settings, Action<EasilyNETMongoParams>? param = null)
        where T : EasilyNETMongoContext
    {
        if (!settings.Servers.Any()) throw new("mongo server address can't be empty!");
        var dbOptions = new EasilyNETMongoOptions();
        var options = new EasilyNETMongoParams();
        param?.Invoke(options);
        options.Options?.Invoke(dbOptions);
        RegistryConventionPack(dbOptions);
        settings.ClusterConfigurator = options.ClusterBuilder ?? settings.ClusterConfigurator;
        var db = EasilyNETMongoContext.CreateInstance<T>(provider, settings, options.DatabaseName, options.ContextParams.ToArray());
        _ = services.AddSingleton(db).AddSingleton(db.Database).AddSingleton(db.Client);
        return services;
    }

    /// <summary>
    /// 通过连接字符串添加DbContext
    /// </summary>
    /// <typeparam name="T">DbContext</typeparam>
    /// <param name="services">IServiceCollection</param>
    /// <param name="provider">IServiceProvider</param>
    /// <param name="connStr">链接字符串</param>
    /// <param name="param">其他参数</param>
    /// <returns></returns>
    public static IServiceCollection AddMongoContext<T>(this IServiceCollection services, IServiceProvider provider, string connStr, Action<EasilyNETMongoParams>? param = null)
        where T : EasilyNETMongoContext
    {
        var options = new EasilyNETMongoParams();
        var dbOptions = new EasilyNETMongoOptions();
        param?.Invoke(options);
        options.Options?.Invoke(dbOptions);
        RegistryConventionPack(dbOptions);
        var mongoUrl = new MongoUrl(connStr);
        var settings = MongoClientSettings.FromUrl(mongoUrl);
        settings.LinqProvider = options.LinqProvider;
        var dbName = !string.IsNullOrWhiteSpace(mongoUrl.DatabaseName) ? mongoUrl.DatabaseName : options.DatabaseName;
        _ = services.AddMongoContext<T>(provider, settings, c =>
        {
            c.ClusterBuilder = options.ClusterBuilder;
            c.DatabaseName = dbName;
            c.ContextParams = options.ContextParams;
        });
        return services;
    }

    private static void RegistryConventionPack(EasilyNETMongoOptions options)
    {
        foreach (var item in options.ConventionRegistry)
            ConventionRegistry.Register(item.Key, item.Value, _ => true);
        if (!options.DefaultConventionRegistry) ConventionRegistry.Remove(options.ConventionRegistry.First().Key);
        ConventionRegistry.Register($"easily-id-pack-{ObjectId.GenerateNewId()}", new ConventionPack
        {
            new StringObjectIdIdGeneratorConvention() //ObjectId → String mapping ObjectId
        }, x => !EasilyNETMongoOptions.ObjIdToStringTypes.Contains(x));
        if (first) return;
        BsonSerializer.RegisterSerializer(new DateTimeSerializer(DateTimeKind.Local)); //to local time
        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128)); //decimal to decimal default
        first = !first;
    }

    private static string ConnectionString(IConfiguration configuration)
    {
        var connStr = configuration["CONNECTIONSTRINGS_MONGO"] ?? configuration.GetConnectionString("Mongo");
        return connStr ?? throw new("💔:no [CONNECTIONSTRINGS_MONGO] env or ConnectionStrings.Mongo is null in appsettings.json");
    }
}