namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 默认EF CORE上下文
/// </summary>
public abstract class DefaultDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// 当前事务
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
    protected DefaultDbContext(DbContextOptions options, IServiceProvider serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider.GetService<ILoggerFactory>()?.CreateLogger<DefaultDbContext>() ?? NullLogger<DefaultDbContext>.Instance;
    }

    /// <summary>
    /// 服务提供者
    /// </summary>

    protected IServiceProvider ServiceProvider { get; private set; }

    private ILogger? Logger { get; }

    /// <summary>
    /// 是否激活事务
    /// </summary>
    public bool HasActiveTransaction => _currentTransaction != null;

    /// <summary>
    /// 异步开启事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// 异步提交并清除当前事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public virtual async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
        {
            await _currentTransaction?.CommitAsync(cancellationToken)!;
            _currentTransaction = default;
        }
    }

    /// <summary>
    /// 异步回滚事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    public virtual async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (HasActiveTransaction)
        {
            await _currentTransaction?.RollbackAsync(cancellationToken)!;
            _currentTransaction = default;
        }
    }

    /// <summary>
    /// 内存释放
    /// </summary>
    public override void Dispose()
    {
        _currentTransaction?.Dispose();
        _currentTransaction = default;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 保存更改操作
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        var count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        Logger?.LogInformation("保存{count}条数据", count);
        return count;
    }
}