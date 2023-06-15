using MongoDB.Driver;

namespace EasilyNET.Mongo.Core.Abstractions;

/// <summary>
/// IMongoContext
/// </summary>
public interface IMongoContext
{
    /// <summary>
    /// 获取IMongoCollection
    /// </summary>
    /// <typeparam name="TDocument">实体</typeparam>
    /// <param name="name">集合名称</param>
    /// <returns></returns>
    IMongoCollection<TDocument> GetCollection<TDocument>(string name);

    /// <summary>
    /// 获取自定义数据库
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IMongoDatabase GetDatabase(string name);

    /// <summary>
    /// 同步方式获取一个已开启事务的Session
    /// </summary>
    /// <returns></returns>
    IClientSessionHandle GetStartedSession();

    /// <summary>
    /// 异步方式获取一个已开启事务的Session
    /// </summary>
    /// <returns></returns>
    Task<IClientSessionHandle> GetStartedSessionAsync();
}