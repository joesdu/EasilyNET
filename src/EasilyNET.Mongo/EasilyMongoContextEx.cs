using MongoDB.Driver;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo;

/// <summary>
/// DbContext的一些方法,便于简化代码
/// </summary>
public partial class EasilyMongoContext
{
    /// <summary>
    /// 获取IMongoCollection
    /// </summary>
    /// <typeparam name="TDocument">实体</typeparam>
    /// <param name="name">集合名称</param>
    /// <returns></returns>
    protected IMongoCollection<TDocument> GetCollection<TDocument>(string name)
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

    /// <summary>
    /// 同步方式获取一个已开启事务的Session
    /// </summary>
    /// <returns></returns>
    public IClientSessionHandle GetStartedSession()
    {
        var session = Client.StartSession();
        session.StartTransaction();
        return session;
    }

    /// <summary>
    /// 异步方式获取一个已开启事务的Session
    /// </summary>
    /// <returns></returns>
    public async Task<IClientSessionHandle> GetStartedSessionAsync()
    {
        var session = await Client.StartSessionAsync();
        session.StartTransaction();
        return session;
    }
}