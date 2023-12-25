using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// IResourceStore实现
/// </summary>
/// <param name="repository"></param>
internal sealed class RepositoryResourceStore(IRepository repository) : IResourceStore
{
    /// <summary>
    /// 仓储
    /// </summary>
    private readonly IRepository Repository = repository;

    /// <summary>
    /// 通过名称查找Api资源
    /// </summary>
    /// <param name="apiResourceNames"></param>
    /// <returns></returns>
    public Task<IEnumerable<ApiResource>> FindApiResourcesByNameAsync(IEnumerable<string> apiResourceNames) => Task.FromResult(Repository.Where<ApiResource>(e => apiResourceNames.Contains(e.Name)).AsEnumerable());

    /// <summary>
    /// 通过Scope名称查找Api资源
    /// </summary>
    /// <param name="scopeNames"></param>
    /// <returns></returns>
    public Task<IEnumerable<ApiResource>> FindApiResourcesByScopeNameAsync(IEnumerable<string> scopeNames) => Task.FromResult(Repository.Where<ApiResource>(e => scopeNames.Contains(e.Name)).AsEnumerable());

    /// <summary>
    /// 通过名称查找ApiScopes
    /// </summary>
    /// <param name="scopeNames"></param>
    /// <returns></returns>
    public Task<IEnumerable<ApiScope>> FindApiScopesByNameAsync(IEnumerable<string> scopeNames) => Task.FromResult(Repository.Where<ApiScope>(e => scopeNames.Contains(e.Name)).AsEnumerable());

    /// <summary>
    /// 通过Scope名称查找Identity资源
    /// </summary>
    /// <param name="scopeNames"></param>
    /// <returns></returns>
    public Task<IEnumerable<IdentityResource>> FindIdentityResourcesByScopeNameAsync(IEnumerable<string> scopeNames) => Task.FromResult(Repository.Where<IdentityResource>(e => scopeNames.Contains(e.Name)).AsEnumerable());

    /// <summary>
    /// 获取所有Resources
    /// </summary>
    /// <returns></returns>
    public Task<Resources> GetAllResourcesAsync() => Task.FromResult(new Resources(GetAllIdentityResources(), GetAllApiResources(), GetAllApiScopes()));

    private IEnumerable<IdentityResource> GetAllIdentityResources() => Repository.All<IdentityResource>();
    private IEnumerable<ApiResource> GetAllApiResources() => Repository.All<ApiResource>();
    private IEnumerable<ApiScope> GetAllApiScopes() => Repository.All<ApiScope>();
}