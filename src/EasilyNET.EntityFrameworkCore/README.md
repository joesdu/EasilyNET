##### EasilyNET.EntityFrameworkCore

## Install Package

```shell
Install-Package EasilyNET.EntityFrameworkCore
```

## 创建实体

```csharp
public sealed class User : Entity<long>, IAggregateRoot, IMayHaveCreator<long?>, IHasCreationTime, IHasModifierId<long?>, IHasModificationTime, IHasDeleterId<long?>, IHasDeletionTime
{
    private User() { }

    public User(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; private set; } = default!;

    public int Age { get; }

    /// <inheritdoc />
    public DateTime CreationTime { get; set; }

    /// <inheritdoc />
    public long? DeleterId { get; set; }

    /// <inheritdoc />
    public DateTime? DeletionTime { get; set; }

    /// <inheritdoc />
    public DateTime? LastModificationTime { get; set; }

    /// <inheritdoc />
    public long? LastModifierId { get; set; }

    /// <inheritdoc />
    public long? CreatorId { get; set; }

    public void ChangeName(string name)
    {
        Name = name;
    }
}

1.Entity 实体需要什么类型Id请自己控制必须的。
2.IAggregateRoot 聚合根假如使用DDD模式的话，请加上这个，不是必须的请自己的需求来
3.IMayHaveCreator 创建人ID，可空的
4.IHasCreationTime 创建时间
5.IHasModifierId  修改人ID
6.IHasModificationTime 修改时间
7.IHasDeleterId 软删除用户ID
8.IHasDeletionTime、IHasSoftDelete 软删除时间，继承IHasSoftDelete，假如需要使用软删除的请就可以了。
 以上1.是必须的，请按自己的需要
```

## 实体映射

```csharp
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(50);
        builder.ToTable("User");
    }
}

以上继承那些接口自动创建。
自动把IEntityTypeConfiguration实体映射器添加到OnModelCreating(ModelBuilder modelBuilder)模型生成器中。
```

## EF 上下文

```csharp
public sealed class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, IServiceProvider? serviceProvider)
        : base(options, serviceProvider)
    {
        Database.EnsureCreated(); //不是必须的，按自己方式，最好使用命令迁移
    }

}
```

## 注册服务

```csharp
AddEFCore<TestDbContext>(options => options.ConfigureDbContextBuilder = builder => { builder.UseSqlite("Data Source=My.db"); });

AddEFCore 方法里面，只注入工作单元，假如要使用仓储请自行注入
```

## 仓储使用

仓储有两种方式

1.

```csharp
注入：
AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
使用
 _serviceProvider.GetService<IRepository<User, long>>();
最好在构造函数下
IRepository<User, long>

```

2.

```csharp
创建:
public interface IUserRepository : IRepository<User, long>;

/// <summary>
/// UserRepository
/// </summary>
/// <param name="dbContext"></param>
public class UserRepository(TestDbContext dbContext) : RepositoryBase<User, long, TestDbContext>(dbContext), IUserRepository;

注入:
AddScoped<IUserRepository, UserRepository>();
```

## 上下文工作单元
 `DefaultDbContext`必须继承

  BeginTransactionAsync 异步开启事务
  CommitTransactionAsync 异步提交当前事务
  RollbackTransactionAsync 异步回滚事务
  ApplyConfigurations 配置实体类型,可以重写
  ConfigureBaseProperties 配置基本属性,添加ConfigureByConvention、ConfigureSoftDelete
  DispatchSaveBeforeEventsAsync 异步调度发生前事件
  GetUserId 得到当前用户Id,保存时候赋值到那些继承的接口字段中，可以使用AddCurrentUser()注入，也可以实现默认接口ICurrentUser
  SetDeletedAudited 设置审计删除，IsDeleted、IHasDeletionTime，IHasDeleterId
  SetModifierAudited 设置审计修改，LastModificationTime、LastModifierId
  SetCreatorAudited 设置审计创建，CreationTime、IMayHaveCreator
  DeleteBefore 删除前操作
  UpdateBefore 更新前删除
  AddBefore 添加前操作 
  后面修改修改拦截器做
  SaveChangesBeforeAsync 异步开始保存更改,自动实现审计接口属性赋值
