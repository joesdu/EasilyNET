using MongoDB.Driver;

// ReSharper disable MemberCanBeProtected.Global

namespace EasilyNET.Mongo.Core;

/// <summary>
/// MongoDB基础DbContext
/// </summary>
public class MongoContext : IDisposable
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
    /// Dispose
    /// </summary>
    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取 <see cref="IMongoCollection{TDocument}" />.
    /// </summary>
    /// <typeparam name="TDocument">实体</typeparam>
    /// <param name="name">集合名称</param>
    /// <returns>
    ///     <see cref="IMongoCollection{TDocument}" />
    /// </returns>
    public IMongoCollection<TDocument> GetCollection<TDocument>(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        return Database.GetCollection<TDocument>(name);
    }

    /// <summary>
    /// 获取自定义数据库
    /// </summary>
    /// <param name="name">集合名称</param>
    /// <returns>
    ///     <see cref="IMongoDatabase" />
    /// </returns>
    public IMongoDatabase GetDatabase(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name, nameof(name));
        return Client.GetDatabase(name);
    }

    /// <summary>
    /// 同步方式获取一个已开启事务的 <see cref="IClientSessionHandle">Session</see>
    /// </summary>
    /// <returns>
    ///     <see cref="IClientSessionHandle" />
    /// </returns>
    public IClientSessionHandle GetStartedSession()
    {
        var session = Client.StartSession();
        session.StartTransaction();
        return session;
    }

    /// <summary>
    /// 异步方式获取一个已开启事务的 <see cref="IClientSessionHandle">Session</see>
    /// </summary>
    /// <returns>
    ///     <see cref="Task{IClientSessionHandle}" />
    /// </returns>
    public async Task<IClientSessionHandle> GetStartedSessionAsync()
    {
        var session = await Client.StartSessionAsync();
        session.StartTransaction();
        return session;
    }

    /// <summary>
    /// 创建 <see cref="MongoContext" /> 子类实例
    /// </summary>
    /// <typeparam name="T">DbContext</typeparam>
    /// <param name="settings">
    ///     <see cref="MongoClientSettings" />
    /// </param>
    /// <param name="dbName">数据库名称</param>
    /// <returns>
    ///     <see cref="MongoContext" />
    /// </returns>
    public static T CreateInstance<T>(MongoClientSettings settings, string dbName) where T : MongoContext
    {
        var t = Activator.CreateInstance<T>();
        t.Client = new MongoClient(settings);
        t.Database = t.Client.GetDatabase(dbName);
        return t;
    }
}