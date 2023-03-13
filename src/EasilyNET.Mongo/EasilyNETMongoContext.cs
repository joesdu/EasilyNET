using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo;

/// <summary>
/// mongodb base context
/// </summary>
public class EasilyNETMongoContext
{
    /// <summary>
    /// MongoClient
    /// </summary>
    public IMongoClient Client { get; private set; } = default!;

    /// <summary>
    /// 获取链接字符串或者HoyoMongoSettings中配置的特定名称数据库或默认数据库hoyo
    /// </summary>
    public IMongoDatabase Database { get; private set; } = default!;

    internal static T CreateInstance<T>(MongoClientSettings settings, string dbName) where T : EasilyNETMongoContext
    {
        var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}