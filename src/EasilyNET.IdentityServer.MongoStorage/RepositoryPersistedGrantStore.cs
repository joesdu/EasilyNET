using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// IPersistedGrantStore实现
/// </summary>
/// <param name="repository"></param>
internal sealed class RepositoryPersistedGrantStore(IRepository repository) : IPersistedGrantStore
{
    /// <summary>
    /// 获取所有
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter) => Task.FromResult(repository.Where<PersistedGrant>(c => c.SubjectId == filter.SubjectId).AsEnumerable());

    /// <summary>
    /// 获取单条数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task<PersistedGrant?> GetAsync(string key) => Task.FromResult(repository.Single<PersistedGrant>(i => i.Key == key))!;

    /// <summary>
    /// 移除所有
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    public Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        repository.Delete<PersistedGrantFilter>(i => i.SubjectId == filter.SubjectId && i.ClientId == filter.ClientId && i.Type == filter.Type);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 移除单条数据
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public Task RemoveAsync(string key)
    {
        repository.Delete<PersistedGrant>(i => i.Key == key);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 持久授权
    /// </summary>
    /// <param name="grant"></param>
    /// <returns></returns>
    public Task StoreAsync(PersistedGrant grant)
    {
        repository.Add(grant);
        return Task.CompletedTask;
    }
}