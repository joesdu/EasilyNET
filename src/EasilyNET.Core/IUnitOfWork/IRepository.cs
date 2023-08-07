using EasilyNET.Core.Entities;
using System.Linq.Expressions;

namespace EasilyNET.Core.IUnitOfWork;

/// <summary>
/// 仓储
/// </summary>
/// <typeparam name="TEntity">实体</typeparam>
/// <typeparam name="TKey">主键</typeparam>
public interface IRepository<TEntity, in TKey>
    where TEntity : class, IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 异步使用主键查询
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">token</param>
    /// <returns>返回查询后实体</returns>
    ValueTask<TEntity> FindAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IQueryable<TEntity> Query(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否存在
    /// </summary>
    /// <param name="predicate">条件</param>
    /// <param name="cancellationToken"></param>
    /// <returns>返回true/false</returns>
    ValueTask<bool> IsExists(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);
}