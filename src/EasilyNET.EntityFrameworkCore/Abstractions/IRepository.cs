



namespace EasilyNET.EntityFrameworkCore;

/// 放在那里好，还是学.NET放到抽象层？？？
/// 还在拿在这里呢？？？？？
/// 会不会出现别的地方也用仓储呢？
/// 放在这里会不会有点改呢？违法设计呢？？？？？？？
/// 暂时放在这里。。。。。
/// <summary>
/// 仓储类
/// </summary>
/// <typeparam name="TEntity">实体</typeparam>
/// <typeparam name="TKey">主键</typeparam>
public interface IRepository<TEntity, in TKey>
    where TEntity : Entity<TKey>,new()
    where TKey : IEquatable<TKey>
{
    
    /// <summary>
    /// 获取工作单元对象
    /// </summary>
    IUnitOfWork UnitOfWork { get; }
    
    /// <summary>
    /// 异步使用主键查询
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="cancellationToken">token</param>
    /// <returns>返回查询后实体</returns>
    ValueTask<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    IQueryable<TEntity?> Query(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步添加
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    ValueTask<TEntity?> AddAsync(TEntity entity);
    
    /// <summary>
    /// 异步更新
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    ValueTask<TEntity?> UpdateAsync(TEntity entity);

    /// <summary>
    /// 异步根据ID删除
    /// </summary>
    /// <param name="id">主建</param>
    /// <returns></returns>
    ValueTask DeleteByIdAsync(TKey id);

    /// <summary>
    /// 异步移除
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    ValueTask RemoveAsync(Entity entity);
}