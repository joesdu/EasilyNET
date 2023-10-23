using MediatR;
using System.Runtime.CompilerServices;

// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 默认EF CORE上下文
/// </summary>
public abstract class DefaultDbContext : DbContext, IUnitOfWork
{
    /// <summary>
    /// 配置基本属性方法
    /// </summary>
    private static readonly MethodInfo? ConfigureBasePropertiesMethodInfo
        = typeof(DefaultDbContext)
            .GetMethod(nameof(ConfigureBaseProperties),
                BindingFlags.Instance | BindingFlags.NonPublic);

 
 


    /// <summary>
    /// 要更改实体基类型
    /// </summary>
    private readonly Type[] _auditedEntryBaseTypes =
    {
        typeof(IHasCreationTime),
        typeof(IMayHaveCreator<>),
        typeof(IHasSoftDelete),
        typeof(IHasDeletionTime),
        typeof(IHasDeleterId<>)
    };

    /// <summary>
    /// 实体值状态数组
    /// </summary>
    private readonly EntityState[] _auditedStates = { EntityState.Added, EntityState.Deleted, EntityState.Modified };

    /// <summary>
    /// 当前事务
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// 中介者发布事件
    /// </summary>
    protected IMediator Mediator { get;  }

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
    protected DefaultDbContext(DbContextOptions options, IServiceProvider? serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<DefaultDbContext>() ?? NullLogger<DefaultDbContext>.Instance;
        Mediator = serviceProvider?.GetService<IMediator>() ?? NullMediator.Instance;
    }

    /// <summary>
    /// 服务提供者
    /// </summary>

    protected IServiceProvider? ServiceProvider { get; }

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

    /// <inheritdoc />
    public async Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default)
    {
        var count = await SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc />
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
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

        await SaveChangesBeforeAsync(cancellationToken);
        var count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        Logger?.LogInformation($"保存{count}条数据");
        await SaveChangesAfterAsync(cancellationToken);
        return count;
    }

     
    /// <summary>
    /// 异步开始保存更改
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual async Task SaveChangesBeforeAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<EntityEntry> entityEntries = ChangeTracker.Entries<Entity>();
        foreach (var entityEntry in entityEntries)
        {
            switch (entityEntry.State)
            {
                case EntityState.Added:
                    AddBefore(entityEntry);
                    break;
                case EntityState.Modified:
                    UpdateBefore(entityEntry);
                    break;
                case EntityState.Deleted:
                    DeleteBefore(entityEntry);
                    break;
            }
        }
        await DispatchSaveBeforeEventsAsync(cancellationToken);
    }
    
    /// <summary>
    /// 添加前操作
    /// </summary>
    /// <param name="entry"></param>
    protected virtual void AddBefore(EntityEntry entry)
    {
     
        SetCreatorAudited(entry);
        SetModifierAudited(entry);
    }
    
    /// <summary>
    /// 更新前删除
    /// </summary>
    /// <param name="entry"></param>
    protected virtual void UpdateBefore(EntityEntry entry)
    {
        SetModifierAudited(entry);
    }

    /// <summary>
    ///  删除前操作
    /// </summary>
    /// <param name="entry"></param>
    protected virtual void DeleteBefore(EntityEntry entry)
    {
     
        SetDeletedAudited(entry);
    }

    /// <summary>
    /// 异步结束保存更改
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    protected virtual Task SaveChangesAfterAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }



    /// <summary>
    /// 设置创建者审计
    /// </summary>
    protected virtual void SetCreatorAudited(EntityEntry entry)
    {
        entry.SetCurrentValue(EFCoreShare.CreationTime, DateTime.Now);
        
        entry.SetPropertyValue(EFCoreShare.CreatorId,GetUserId());
        // if (entity is IMayHaveCreator<long> creatorLong)
        // {
        //     creatorLong.CreatorId = ChangeType<long>(GetUserId());
        //     return;
        // }
        //
        // if (entity is IMayHaveCreator<long?> creatorNullLong)
        // {
        //
        //     creatorNullLong.CreatorId = ChangeType<long?>(GetUserId());
        //     return;
        // }
        //
        // if (entity is IMayHaveCreator<int> creatorInt)
        // {
        //
        //     creatorInt.CreatorId = ChangeType<int>(GetUserId());
        //     return;
        // }
        //
        // if (entity is IMayHaveCreator<int?> creatorNullInt)
        // {
        //
        //     creatorNullInt.CreatorId = ChangeType<int?>(GetUserId());
        //     return;
        // }

        
        // entry.SetCurrentValue(EFCoreShare.CreatorId);
 
    }

    
    /// <summary>
    /// 设置修改审计
    /// </summary>
    protected virtual void SetModifierAudited(EntityEntry entry)
    {
        entry.SetPropertyValue(EFCoreShare.ModifierId,GetUserId());
        entry.SetCurrentValue(EFCoreShare.ModificationTime, DateTime.Now);

      
    }
    /// <summary>
    /// 设置删除
    /// </summary>
    protected virtual void SetDeletedAudited(EntityEntry entry)
    {
        entry.SetCurrentValue(EFCoreShare.IsDeleted, true);
        entry.SetCurrentValue(EFCoreShare.DeletionTime, DateTime.Now);
        entry.SetPropertyValue(EFCoreShare.DeleterId,GetUserId());
        entry.State = EntityState.Modified;
    }

    /// <summary>
   /// 配置模型
   /// </summary>
   /// <param name="modelBuilder"></param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ApplyConfigurations(modelBuilder);
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureBasePropertiesMethodInfo?.MakeGenericMethod(entityType.ClrType).Invoke(this, new object[] { modelBuilder, entityType });
        }
    }
    
    /// <summary>
    /// 配置实体类型
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected virtual void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    /// <summary>
    /// 配置基本属性
    /// </summary>
    /// <param name="modelBuilder"></param>
    /// <param name="mutableEntityType"></param>
    /// <typeparam name="TEntity"></typeparam>
    protected virtual void ConfigureBaseProperties<TEntity>(ModelBuilder modelBuilder, IMutableEntityType mutableEntityType)
        where TEntity : class
    {
        if (mutableEntityType.IsOwned())
        {
            return;
        }
        if (!typeof(Entity).IsAssignableFrom(typeof(TEntity)))
        {
            return;
        }
        modelBuilder.Entity<TEntity>().ConfigureByConvention();
        modelBuilder.Entity<TEntity>().ConfigureSoftDelete();
    }
    
    /// <summary>
    /// 异步调度发生前事件
    /// </summary>
    protected virtual async Task DispatchSaveBeforeEventsAsync(CancellationToken cancellationToken = default)
    {
        await Mediator.DispatchDomainEventsAsync(this, cancellationToken);
    }
    


    /// <summary>
    /// 得到当前用户
    /// </summary>
    /// <returns></returns>
    protected virtual string GetUserId()
    {
        return default!;
    }
}

/// <summary>
/// 空的Mediator
/// </summary>
public sealed class NullMediator : IMediator
{
    /// <summary>
    /// 实例
    /// </summary>
    public static readonly NullMediator Instance = new NullMediator();
    /// <inheritdoc />
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = new CancellationToken()) => Task.FromResult<TResponse>(default!);

    /// <inheritdoc />
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = new CancellationToken()) where TRequest : IRequest => Task.FromResult(false);

    /// <inheritdoc />
    public Task<object?> Send(object request, CancellationToken cancellationToken = new CancellationToken()) => Task.FromResult(default(object?));

    /// <inheritdoc />
    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = new CancellationToken()) => default(IAsyncEnumerable<TResponse>)!;

    /// <inheritdoc />
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = new CancellationToken()) =>  default(IAsyncEnumerable<object?>)!;

    /// <inheritdoc />
    public Task Publish(object notification, CancellationToken cancellationToken = new CancellationToken()) => Task.CompletedTask;

    /// <inheritdoc />
    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = new CancellationToken()) where TNotification : INotification => Task.CompletedTask;
}