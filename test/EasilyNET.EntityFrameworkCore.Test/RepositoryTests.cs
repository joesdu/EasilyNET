




namespace EasilyNET.EntityFrameworkCore.Test;
[TestClass]
public class RepositoryTests
{
 

    //本人太笨了NSubstitute 测试怎么也学不会。。。。
    private DbContextOptions<TestDbContext> DummyOptions { get; } = new DbContextOptionsBuilder<TestDbContext>().UseSqlite("Data Source=My.db").Options;

    private IServiceCollection _serviceCollection;
    public  RepositoryTests()
    {

        // // var dbContextMock = new DbContextMock<TestDbContext>();
        // _serviceCollection.AddDbContext<TestDbContext>(o
        //     => o.UseSqlite("Data Source=My.db"));
        // _serviceCollection.AddScoped<IUserRepository,UserRepository>();
        // _serviceProvider= _serviceCollection.BuildServiceProvider();
    
        
        // var options = new DbContextOptionsBuilder<TestDbContext>()
        //               .UseSqlite("Data Source=My.db")
        //               .Options;
        // _dbContextOptions = options;
 
        // var dbContext = new TestDbContext(options,serviceProvider);
        // 创建一个模拟的 IServiceProvider
        // var serviceProvider = Substitute.For<IServiceProvider>();
        // var dbContext =  Substitute.For<TestDbContext>(DummyOptions,serviceProvider);
        // dbContext.Database.EnsureCreated(); 
        
    }

    [TestMethod]
    public async Task AddUserAsync_ShouldAddUserToDatabase()
    {
        
       // Arrange
        TestDbContext dbContext = new TestDbContext(DummyOptions,null);
        IUserRepository userRepository = new UserRepository(dbContext);
        // // Act
        var user = new User("大黄瓜", 18);
        await userRepository.AddAsync(user);
        //
        int count= await userRepository.UnitOfWork.SaveChangesAsync();
        // // Assert
         var addedUser = await userRepository.FindAsync(user.Id);
        Assert.IsNotNull(addedUser);
    }
}

public sealed class User : Entity<long>,IAggregateRoot
{
    private User()
    {
        
    }
    public User(string name, int age)
    {

        Name = name;
        Age = age;
    }


    public string Name { get; }

    public int Age { get; }
    

}

public class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, IServiceProvider? serviceProvider)
        : base(options,serviceProvider)
    {
         Database.EnsureCreated();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}

public interface IUserRepository : IRepository<User, long>
{
    
}
public class UserRepository:RepositoryBase<User,long,TestDbContext>,IUserRepository
{
    /// <inheritdoc />
    public UserRepository(TestDbContext dbContext) : base(dbContext)
    {
        
    }


}

/// <summary>
/// 用户ID
/// </summary>
/// <param name="Id"></param>
public record UserId(long Id)
{


    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator long(UserId id) => id.Id;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static implicit operator UserId(long id) => new UserId(id);

    
    /// <summary>
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Id.ToString();
}

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

