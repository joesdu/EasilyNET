using EasilyNET.IdentityServer.MongoStorage.Entities;
using System.Linq.Expressions;

namespace EasilyNET.IdentityServer.MongoStorage.Abstraction;

/// <summary>
/// IPersistedGrantDbContext
/// </summary>
public interface IPersistedGrantDbContext : IDisposable
{
    /// <summary>
    /// Queryable PersistedGrants
    /// </summary>
    IQueryable<PersistedGrant> PersistedGrants { get; }

    /// <summary>
    /// Remove
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    Task Remove(Expression<Func<PersistedGrant, bool>> filter);

    /// <summary>
    /// RemoveExpired
    /// </summary>
    /// <returns></returns>
    Task RemoveExpired();

    /// <summary>
    /// InsertOrUpdate
    /// </summary>
    /// <param name="filter"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task InsertOrUpdate(Expression<Func<PersistedGrant, bool>> filter, PersistedGrant entity);
}