using System.Linq.Expressions;

namespace EasilyNET.Core.Domains;

/// <summary>
/// 仓储类
/// </summary>
/// <typeparam name="TEntity">实体</typeparam>
/// <typeparam name="TKey">主键</typeparam>
public interface IRepository<TEntity, in TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 获取工作单元对象
    /// </summary>
    IUnitOfWork UnitOfWork { get; }

    /// <summary>
    /// 查询实体
    /// </summary>
    IQueryable<TEntity> FindEntity { get; }

    /// <summary>
    /// 异步使用主键查询
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">token</param>
    /// <returns>返回查询后实体</returns>
    ValueTask<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 带条件查询
    /// </summary>
    /// <param name="predicate">条件</param>
    /// <returns></returns>
    IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);

    /// <summary>
    /// 异步添加
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步添加更新
    /// </summary>
    /// <param name="entity">实体</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default);
    /// <summary>
    /// 更新
    /// </summary>
    /// <param name="entity">动态实体</param>
    /// <returns></returns>
    void Update(TEntity entity);

    /// <summary>
    /// 异步
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    void Remove(TEntity entity);
}