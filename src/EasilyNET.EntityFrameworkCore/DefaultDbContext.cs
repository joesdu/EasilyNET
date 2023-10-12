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
    private readonly Type[] _changeEntryBaseTypes =
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
    private readonly EntityState[] _entryValueStates = { EntityState.Added, EntityState.Deleted, EntityState.Modified };

    /// <summary>
    /// 更改实体值字典
    /// </summary>
    private readonly Dictionary<EntityState, Action<EntityState, EntityEntry>> changeEntryValueDic = new()
    {
        {
            EntityState.Added, (state, entry) =>
            {
                entry.SetCurrentValue(EFCoreShare.CreatorId);
                entry.SetCurrentValue(EFCoreShare.CreationTime, DateTime.Now);
            }
        },
        {
            EntityState.Modified, (state, entry) =>
            {
                entry.SetCurrentValue(EFCoreShare.ModifierId);
                entry.SetCurrentValue(EFCoreShare.ModificationTime, DateTime.Now);
            }
        },
        {
            EntityState.Deleted, (state, entry) =>
            {
                entry.SetCurrentValue(EFCoreShare.IsDeleted, true);
                entry.SetCurrentValue(EFCoreShare.DeletionTime, DateTime.Now);
                entry.SetCurrentValue(EFCoreShare.DeleterId);
                entry.State = EntityState.Modified;
            }
        }
    };

    /// <summary>
    /// 当前事务
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// </summary>
    /// <param name="options"></param>
    /// <param name="serviceProvider"></param>
    protected DefaultDbContext(DbContextOptions options, IServiceProvider? serviceProvider) : base(options)
    {
        ServiceProvider = serviceProvider;
        Logger = serviceProvider?.GetService<ILoggerFactory>()?.CreateLogger<DefaultDbContext>() ?? NullLogger<DefaultDbContext>.Instance;
    }

    /// <summary>
    /// 服务提供者
    /// </summary>

    protected IServiceProvider? ServiceProvider { get; private set; }

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
        BeforeSaveChangeAsync();
        SavingChanges += SavingChanges_ChangeEntryValue;
        var count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        Logger?.LogInformation("保存{count}条数据", count);
        return count;
    }

    /// <summary>
    /// 保存改时，根据状态更改实体的值
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected virtual void SavingChanges_ChangeEntryValue(object? sender, SavingChangesEventArgs e)
    {
        //继承Entity才处理
        IEnumerable<EntityEntry> entityEntries = ChangeTracker.Entries<Entity>();
        foreach (var entityEntry in
                 entityEntries.Where(o =>
                     _entryValueStates.Contains(o.State) &&
                     _changeEntryBaseTypes.Any(type => o.Entity.GetType().IsDeriveClassFrom(type))))
        {
            var state = entityEntry.State;
            var entity = entityEntry.Entity;
            changeEntryValueDic[state](state, entityEntry);
            // switch (state)
            // {
            //     case EntityState.Added :
            //         entityEntry.SetCurrentValue(EFCoreShare.CreatorId);
            //         entityEntry.SetCurrentValue(EFCoreShare.CreationTime,DateTime.Now);
            //         break;
            //     
            //     case EntityState.Modified  :
            //         entityEntry.SetCurrentValue(EFCoreShare.ModifierId);
            //         entityEntry.SetCurrentValue(EFCoreShare.ModificationTime,DateTime.Now);
            //         break;
            //     
            //     case EntityState.Deleted:
            //         entityEntry.SetCurrentValue(EFCoreShare.IsDeleted,true);
            //         entityEntry.SetCurrentValue(EFCoreShare.DeletionTime,DateTime.Now);
            //         entityEntry.SetCurrentValue(EFCoreShare.DeleterId);
            //         entityEntry.State = EntityState.Modified;
            //         break;
            // }
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            ConfigureBasePropertiesMethodInfo?.MakeGenericMethod(entityType.ClrType).Invoke(this, new object[] { modelBuilder, entityType });
        }
    }

    /// <summary>
    /// 开始保存操作
    /// </summary>
    protected virtual void BeforeSaveChangeAsync()
    {
        // IEnumerable<EntityEntry> entityEntries=  ChangeTracker.Entries<Entity>();
        // ChangeEntityState(entityEntries);
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
}