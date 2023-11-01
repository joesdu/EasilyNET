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

以上那些继承接口，自动映射。
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
最新在构造函数下
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
