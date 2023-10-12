namespace EasilyNET.EntityFrameworkCore.Repositories;

/// <summary>
/// </summary>
/// <typeparam name="TEntity"></typeparam>
/// <typeparam name="TKey"></typeparam>
public class Repository<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// </summary>
    /// <param name="dbContext"></param>
    public Repository(DefaultDbContext dbContext)
    {
        DbContext = dbContext;
    }

    /// <summary>
    /// 上下文
    /// </summary>
    private DefaultDbContext DbContext { get; }

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
    public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? predicate = null) => predicate is not null ? FindEntityQueryable.Where(predicate) : FindEntityQueryable;

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await EntitySet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        EntitySet.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        EntitySet.Remove(entity);
    }
}

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
    public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? predicate = null) => predicate is not null ? FindEntityQueryable.Where(predicate) : FindEntityQueryable;

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await EntitySet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Update(TEntity entity)
    {
        EntitySet.Update(entity);
    }

    /// <inheritdoc />
    public void Remove(TEntity entity)
    {
        EntitySet.Remove(entity);
    }
}