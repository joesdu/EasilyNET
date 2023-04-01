using Microsoft.Extensions.DependencyInjection;
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
    /// 获取链接字符串或者HoyoMongoSettings中配置的特定名称数据库或默认数据库
    /// </summary>
    public IMongoDatabase Database { get; private set; } = default!;

    /// <summary>
    /// 获取IMongoCollection
    /// </summary>
    /// <typeparam name="TDocument">实体</typeparam>
    /// <param name="name">集合名称</param>
    /// <returns></returns>
    protected IMongoCollection<TDocument> Collection<TDocument>(string name)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
#else
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
#endif
        return Database.GetCollection<TDocument>(name);
    }

    /// <summary>
    /// 获取自定义数据库
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public IMongoDatabase GetDatabase(string name)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
#else
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentNullException(nameof(name));
#endif
        return Client.GetDatabase(name);
    }

    internal static T CreateInstance<T>(IServiceProvider provider, MongoClientSettings settings, string dbName, params object[] parameters) where T : EasilyNETMongoContext
    {
        // 可支持非默认无参构造函数的DbContext
        var t = ActivatorUtilities.CreateInstance<T>(provider, parameters);
        // var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}