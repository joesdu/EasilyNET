using EasilyNET.IdentityServer.MongoStorage.Entities;

namespace EasilyNET.IdentityServer.MongoStorage.Abstraction;

/// <summary>
/// IConfigurationDbContext
/// </summary>
public interface IConfigurationDbContext : IDisposable
{
    /// <summary>
    /// Queryable Clients
    /// </summary>
    IQueryable<Client> Clients { get; }

    /// <summary>
    /// Queryable IdentityResources
    /// </summary>
    IQueryable<IdentityResource> IdentityResources { get; }

    /// <summary>
    /// Queryable ApiResources
    /// </summary>
    IQueryable<ApiResource> ApiResources { get; }

    /// <summary>
    /// Queryable ApiScopes
    /// </summary>
    IQueryable<ApiScope> ApiScopes { get; }

    /// <summary>
    /// AddClient
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddClient(Client entity);

    /// <summary>
    /// AddIdentityResource
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddIdentityResource(IdentityResource entity);

    /// <summary>
    /// AddApiResource
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddApiResource(ApiResource entity);

    /// <summary>
    /// AddApiScope
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddApiScope(ApiScope entity);
}