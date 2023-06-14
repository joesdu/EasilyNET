using MongoDB.Driver;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core;

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

    /// <summary>
    /// 创建EasilyMongoContext实例
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="settings"></param>
    /// <param name="dbName"></param>
    /// <returns></returns>
    public static T CreateInstance<T>(MongoClientSettings settings, string dbName) where T : EasilyMongoContext
    {
        var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}