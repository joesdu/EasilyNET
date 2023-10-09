namespace EasilyNET.EntityFrameworkCore.Test;

[TestClass]
public class RepositoryTests
{
    //本人太笨了NSubstitute 测试怎么也学不会。。。。
    // private DbContextOptions<TestDbContext> DummyOptions { get; } = new DbContextOptionsBuilder<TestDbContext>().UseSqlite("Data Source=My.db").Options;

    private readonly IServiceCollection _serviceCollection = new ServiceCollection();
    private readonly IServiceProvider _serviceProvider;

    public RepositoryTests()
    {
        _serviceCollection.AddDbContext<TestDbContext>(options => options.UseSqlite("Data Source=My.db"));
        _serviceCollection.AddScoped<IUserRepository, UserRepository>();
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    public async Task AddUserAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        // // Act
        var user = new User("大黄瓜", 18);
        await userRepository.AddAsync(user);
        await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        var addedUser = await userRepository.FindAsync(user.Id);
        Assert.IsNotNull(addedUser);
    }

    [TestMethod]
    public async Task UpdateUserAsync_ShouldUpdateUserToDatabase()
    {
        // Arrange
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        // Act
        var user = await userRepository.Query(o => o.Name == "大黄瓜").AsTracking().FirstOrDefaultAsync();
        user?.ChangeName("大黄瓜_01");
        await userRepository.UpdateAsync(user!);
        await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        var newUser = await userRepository.FindAsync(user!.Id);
        Assert.IsTrue(newUser?.Equals(user));
    }
}

public sealed class User : Entity<long>, IAggregateRoot, IHasSoftDelete
{
    private User() { }

    public User(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public string Name { get; private set; } = default!;

    public int Age { get; }

    public void ChangeName(string name)
    {
        Name = name;
    }
}

public sealed class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, IServiceProvider? serviceProvider)
        : base(options, serviceProvider)
    {
        Database.EnsureCreated();
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        AddIsDeletedField(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}

public interface IUserRepository : IRepository<User, long>;

/// <summary>
/// UserRepository
/// </summary>
/// <param name="dbContext"></param>
public class UserRepository(TestDbContext dbContext) : RepositoryBase<User, long, TestDbContext>(dbContext), IUserRepository;

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