#### EasilyNET.Core.Domains

领域相关东西

##### Nuget

使用 Nuget 包管理工具添加依赖包 EasilyNET.Core.Domains

##### 核心
- 实体(Entity、Entity<>)（必须的） 有唯一标识，通过ID判断相等性，增删查改、持续化，可变的，使用IRepository访问数据时必须继承,可以自定义ID类型。
```csharp

public class User : Entity<long>
{
   public string Name{get;set;}
   public Test Test {get;set;}
}
```
- 聚合根(IAggregateRoot) 表示聚合根
```csharp
表示用户聚合
public class User : Entity<long>,IAggregateRoot
{
   public string Name{get;set;}
   
   public List<UserRole> UserRoles {get;set;}
}

表示角色聚合
public class Role : Entity<long>,IAggregateRoot
{
   public string Name{get;set;}
}

//举例使用值对象
public class UserRole : ValueObject
{

  public long UserId{get;set;}
  public long RoleId{get;set;}
  public override IEnumerable<object> GetAtomicValues()
  {
      yield return UserId;
      yield return RoleId;
  }
}
```
- 值对象(ValueObject)无唯一标识，不可以变的，通过属性判断相等性，即时创建、用完即扔。
```csharp

  public sealed class Test(int a, string b, string c) : ValueObject
    {
        private int A { get; } = a;

        private string B { get; } = b;

        private string C { get; } = c;

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return A;
            yield return B;
            yield return C;
        }
    }
```

- 创建者ID(IMayHaveCreator)
```csharp

  public sealed class xxxxx : IMayHaveCreator<long?>
    {
      
        public  long? CreatorId { get; set; }
    }
```
- 创建时间(IHasCreationTime)
```csharp

  public sealed class xxxxx : IHasCreationTime
    {
      
        public DateTime CreationTime { get; set; }
    }
```
- 修改者ID(IHasModifierId)
```csharp

  public sealed class xxxxx : IHasModifierId<long?>
    {
      
        public long? LastModifierId { get; set; }
    }
```
- 修改时间(IHasModificationTime)
```csharp

  public sealed class xxxxx : IHasModificationTime
    {
      
        public DateTime LastModificationTime { get; set; }
    }
```
- 软删除(IHasSoftDelete)
```csharp
   因为这个属性只是查询时候用到，所以在EF Core低层实现，查询时候过滤已删除数据，假如是删除，把字段设置已删除，而不是把数据删除
   public sealed class xxxxx : IHasSoftDelete
    {
      

    }
```
- 删除时间(IHasDeletionTime)，他继承了IHasSoftDelete接口，如果使用该接口，就不用添加IHasSoftDelete接口
```csharp
   因为这个属性只是查询时候用到，所以在EF Core低层实现，查询时候过滤已删除数据，假如是删除，把字段设置已删除，而不是把数据删除
   public sealed class xxxxx : IHasDeletionTime
    {
      
       public DateTime? DeletionTime { get; set; }
    }
```
- 删除者Id(IHasDeleterId)
```csharp
  
   public sealed class xxxxx : IHasDeleterId<long?>
    {
      
       public long? DeleterId { get; set; }
    }
```
- 仓储 (IRepository)实现增删改查，必须继承Entity抽象类才可以使用
```csharp
     本层只是抽象

     _serviceCollection.AddScoped<IUserRepository, UserRepository>();

    
     _serviceCollection.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
     可以使用以下方式：
     ---------------------------------------------
     添加依赖包 EasilyNET.EntityFrameworkCore
     _serviceCollection.AddRepository(); 只注入IRepository<,>，不注入IUserRepository，IUserRepository可以使用自动注入
     ---------------------------------------------

     public interface IUserRepository : IRepository<User, long>;

     /// <summary>
     /// UserRepository
     /// </summary>
     /// <param name="dbContext"></param>
     public class UserRepository(TestDbContext dbContext) : RepositoryBase<User, long, TestDbContext>(dbContext), IUserRepository;

     调用IRepository<User,Long>、IUserRepository
```
- 领域事件 (IDomainEvent)
```csharp
 注册中者中间件，低层是使用MediatR实现的
 Service.AddMediatR(cfg => { cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()); });

internal sealed record AddUserDomainEvent(User User) : IDomainEvent;

internal sealed class AddUserDomainEventHandler : IDomainEventHandler<AddUserDomainEvent>
{
    /// <inheritdoc />
    public Task Handle(AddUserDomainEvent notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"创建用户{notification.User.Id}_{notification.User.Name}");
        return Task.CompletedTask;
    }
}


public sealed class User : Entity<long>
{
    private User() { }

    public User(string name, int age)
    {
        Name = name;
        Age = age;
        AddDomainEvent(new AddUserDomainEvent(this));  //添加领域事件
    }

    public string Name { get; private set; } = default!;

    public int Age { get; }

}

** 如果这里添加，领域事件的话，当IUnitOfWork.SaveChangesAsync()时侯会发布领域事件，不用手动发布。
```
