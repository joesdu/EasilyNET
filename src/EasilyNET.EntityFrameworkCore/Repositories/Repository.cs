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
    public virtual IQueryable<TEntity> FindEntity => EntitySet;

    /// <inheritdoc />
    public IUnitOfWork UnitOfWork => DbContext;

    /// <inheritdoc />
    public ValueTask<TEntity?> FindAsync(TKey id, CancellationToken cancellationToken = default) => DbContext.Set<TEntity>().FindAsync([id], cancellationToken);

    /// <inheritdoc />
    public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate) => FindEntity.Where(predicate);

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await EntitySet.AddAsync(entity, cancellationToken);
    }

    /// <inheritdoc />
    public Task AddOrUpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        if (entity.Id.Equals(default))
        {
            return AddAsync(entity, cancellationToken);
        }
        Update(entity);
        return Task.CompletedTask;
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
    Repository<TEntity, TKey>
    where TEntity : Entity<TKey>, IAggregateRoot
    where TKey : IEquatable<TKey>
    where TDbContext : DefaultDbContext
{
    /// <summary>
    /// </summary>
    /// <param name="dbContext"></param>
    protected RepositoryBase(TDbContext dbContext) : base(dbContext) { }
}