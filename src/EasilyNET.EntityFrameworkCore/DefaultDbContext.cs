




namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 默认EF CORE上下文
/// </summary>
public abstract class DefaultDbContext : DbContext, IUnitOfWork
{

    /// <summary>
    /// 是否删除
    /// </summary>
    public const string IsDeleted = nameof(IsDeleted);
    
    /// <summary>
    /// 创建时间
    /// </summary>
    public const string CreatedDateTime= nameof(CreatedDateTime);
    
    private  MethodInfo  _methodInfo= typeof(EF).GetMethod(nameof(EF.Property))!.MakeGenericMethod(typeof(bool));
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
    /// 当前事务
    /// </summary>
    private IDbContextTransaction? _currentTransaction;

    /// <summary>
    /// 服务提供者
    /// </summary>

    protected IServiceProvider? ServiceProvider { get; private set; }

    private ILogger? Logger { get; }

    /// <summary>
    /// 是否激活事务
    /// </summary>
    public bool HasActiveTransaction =>_currentTransaction != null;

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
    public virtual async  Task CommitTransactionAsync(CancellationToken cancellationToken = default)
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
    /// 保存更改操作
    /// </summary>
    /// <param name="acceptAllChangesOnSuccess"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        int count = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        Logger?.LogInformation($"保存{count}条数据");
        return count;
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
     
        base.OnModelCreating(modelBuilder);
    }
    
    /// <summary>
    /// 动态获取实体表
    /// </summary>
    /// <param name="modelBuilder"></param>
    protected virtual void OnMapEntityTypes(ModelBuilder modelBuilder)
    {
      
        // var baseType = typeof(IEntityTypeConfiguration<>);
        //
        // var assemblys = AssemblyHelper.FindTypes(o => o.IsClass && o.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == baseType)).ToList();
        //
        // if (assemblys.Any())
        // {
        //
        //     assemblys.ForEach(x =>
        //     {
        //
        //         if (modelBuilder.Model.FindEntityType(x) is null)
        //         {
        //             modelBuilder.Model.AddEntityType(x);
        //         }
        //
        //     });
        //     
        // }
        
    }


    
    /// <summary>
    /// 设置软删除字段
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void AddIsDeletedField(ModelBuilder builder)
    {
       var types=  builder.Model.GetEntityTypes().Where(o=>typeof(IHasSoftDelete).IsAssignableFrom(o.ClrType)).ToList();
       foreach (var type in types)
       {
           
           builder.Entity(type.ClrType).Property<bool>(IsDeleted);
           builder.Entity(type.ClrType).HasQueryFilter(GetDeleteLambda((type.ClrType)));
       }
    }
    
    /// <summary>
    /// 设置创建时间字段
    /// </summary>
    /// <param name="builder"></param>
    protected virtual void AddCreateTimeField(ModelBuilder builder)
    {
    
    }

    /// <summary>
    /// 获取过滤条件
    /// </summary>
    /// <param name="clrType"></param>
    /// <returns></returns>
    private LambdaExpression GetDeleteLambda(Type clrType)
    {
        var param = Expression.Parameter(clrType, "it");

        //EF.Property<bool>(it, "IsDeleted")
        Expression call = Expression.Call(_methodInfo, param, Expression.Constant(IsDeleted));

        //(EF.Property<bool>(it, "IsDeleted") == False)
        var binaryExpression = Expression.MakeBinary(ExpressionType.Equal, call, Expression.Constant(false, typeof(bool)));

        // it => EF.Property<bool>(it, "Deleted") == False
        var lambda = Expression.Lambda(binaryExpression, param);
        return lambda;
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
    
}