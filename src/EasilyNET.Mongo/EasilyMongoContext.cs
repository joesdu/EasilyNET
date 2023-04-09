using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo;

/// <summary>
/// mongodb base context
/// </summary>
public partial class EasilyMongoContext
{
    /// <summary>
    /// MongoClient
    /// </summary>
    public IMongoClient Client { get; private set; } = default!;

    /// <summary>
    /// 获取链接字符串或者HoyoMongoSettings中配置的特定名称数据库或默认数据库
    /// </summary>
    public IMongoDatabase Database { get; private set; } = default!;

    internal static T CreateInstance<T>(IServiceProvider provider, MongoClientSettings settings, string dbName, params object[] parameters) where T : EasilyMongoContext
    {
        // 可支持非默认无参构造函数的DbContext
        var t = ActivatorUtilities.CreateInstance<T>(provider, parameters);
        // var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}