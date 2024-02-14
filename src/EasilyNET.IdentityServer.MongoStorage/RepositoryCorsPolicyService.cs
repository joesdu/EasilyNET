using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;

namespace EasilyNET.IdentityServer.MongoStorage;

/// <summary>
/// ICorsPolicyService实现
/// </summary>
/// <param name="repository"></param>
internal sealed class RepositoryCorsPolicyService(IRepository repository) : ICorsPolicyService
{
    private readonly string[] _allowedOrigins = repository.All<Client>().SelectMany(x => x.AllowedCorsOrigins).ToArray();

    /// <summary>
    /// 是否是允许的源
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public Task<bool> IsOriginAllowedAsync(string origin) => Task.FromResult(_allowedOrigins.Contains(origin));
}
