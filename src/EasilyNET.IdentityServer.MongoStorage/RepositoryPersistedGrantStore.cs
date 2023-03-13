using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// IPersistedGrantStore实现
/// </summary>
public class RepositoryPersistedGrantStore : IPersistedGrantStore
{
    /// <summary>
    /// 仓储
    /// </summary>
    private readonly IRepository Repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="repository"></param>
    public RepositoryPersistedGrantStore(IRepository repository)
    {
        Repository = repository;
    }

    /// <summary>
    /// 获取所有
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter) => Task.FromResult(Repository.Where<PersistedGrant>(c => c.SubjectId == filter.SubjectId).AsEnumerable());

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<PersistedGrant> GetAsync(string key) => Task.FromResult(Repository.Single<PersistedGrant>(i => i.Key == key));

    /// <summary>
    /// 移除所有
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        Repository.Delete<PersistedGrantFilter>(i => i.SubjectId == filter.SubjectId && i.ClientId == filter.ClientId && i.Type == filter.Type);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 移除单条数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task RemoveAsync(string key)
    {
        Repository.Delete<PersistedGrant>(i => i.Key == key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 持久授权
    /// </summary>
    /// <param name="grant"></param>
    /// <returns></returns>
    public Task StoreAsync(PersistedGrant grant)
    {
        Repository.Add(grant);
        return Task.CompletedTask;
    }
}