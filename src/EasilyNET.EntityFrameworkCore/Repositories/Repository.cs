namespace EasilyNET.EntityFrameworkCore.Repositories;

/// <summary>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
/// <typeparam name="TDbContext"></typeparam>
public abstract class RepositoryBase<TEntity, TKey, TDbContext> :
    IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
    where TKey : IEquatable<TKey>
    where TDbContext : DefaultDbContext

{
    /// <summary>
    /// </summary>
    /// <param name="dbContext"></param>
    protected RepositoryBase(TDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// 上下文
    /// </summary>
    protected virtual TDbContext DbContext { get; }

    /// <summary>
    /// 表
    /// </summary>
    private DbSet<TEntity> EntitySet => DbContext.Set<TEntity>();

    /// <summary>
    /// 查询实体
    /// </summary>
    public virtual IQueryable<TEntity> FindEntityQueryable => EntitySet;

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => DbContext;

    /// <inheritdoc />
    public ValueTask<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default) => DbContext.Set<TEntity>().FindAsync(new object[] { id }, cancellationToken);

    /// <inheritdoc />
    public IQueryable<TEntity?> Query(Expression<Func<TEntity, bool>>? predicate = null)
    {
        if (predicate is not null)
        {
            return FindEntityQueryable.Where(predicate);
        }
        return FindEntityQueryable;
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await EntitySet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task UpdateAsync(TEntity entity)
    {
        EntitySet.Update(entity);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task RemoveAsync(TEntity entity)
    {
        EntitySet.Remove(entity);
        return Task.CompletedTask;
    }
}