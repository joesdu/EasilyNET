using EasilyNET.EntityFrameworkCore.Extensions;
using System.Collections.Generic;

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
        _serviceCollection.AddDbContext<TestDbContext>(options => { options.UseSqlite("Data Source=My.db"); });
        _serviceCollection.AddScoped<IUserRepository, UserRepository>();
        _serviceCollection.AddScoped(typeof(IRepository<,>),typeof(Repository<,>));
        // _serviceCollection.AddScoped<IRepository<Role, long>, Repository<Role, long>>();
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }

    [TestMethod]
    public async Task AddUserAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        for (int i = 0; i < 10; i++)
        {
            var user = new User($"大黄瓜_{i}", 18);
            await userRepository.AddAsync(user);

        }
        // Act
        int count = await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        Assert.IsTrue(count > 0);
    }

    [TestMethod]
    public async Task UpdateUserAsync_ShouldUpdateUserToDatabase()
    {
        // Arrange
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        // Act
        var user = await userRepository.FindEntityQueryable.AsTracking().FirstOrDefaultAsync();
        user?.ChangeName("大黄瓜_Test");
        userRepository.Update(user!);
        await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        var newUser = await userRepository.FindAsync(user!.Id);
        Assert.IsTrue(newUser?.Equals(user));
    }
    
    [TestMethod]
    public async Task DeleteUserAsync_ShouldDeleteUserToDatabase()
    {
        // Arrange
        var userRepository = _serviceProvider.GetRequiredService<IUserRepository>();
        // Act
        var user = await userRepository.FindEntityQueryable.FirstOrDefaultAsync();
        userRepository.Remove(user!);
        int count= await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert

        Assert.IsTrue(count == 1);
    }
    
    /// <summary>
    /// 添加角色
    /// </summary>
    [TestMethod]
    public async Task AddRoleAsync_ShouldAddRoleToDatabase()
    {

        // Arrange
        var roleRepository = _serviceProvider.GetService<IRepository<Role,long>>();
        for (int i = 0; i < 10; i++)
        {
            var role = new Role($"大黄瓜_{i}");
            await roleRepository!.AddAsync(role);

        }
        // Act
        int count = await roleRepository!.UnitOfWork.SaveChangesAsync();
        // Assert
        Assert.IsTrue(count > 0);
    }
}

public sealed class User : Entity<long>, IAggregateRoot, IMayHaveCreator<long?>,IHasCreationTime,IHasModifierId<long?>,IHasModificationTime,IHasDeleterId<long?>,IHasDeletionTime
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

    /// <inheritdoc />
    public long? CreatorId { get;  }

    /// <inheritdoc />
    public DateTime CreationTime { get; }

    /// <inheritdoc />
    public long? LastModifierId { get; }

    /// <inheritdoc />
    public DateTime? LastModificationTime { get; }

    /// <inheritdoc />
    public long? DeleterId { get; }

    /// <inheritdoc />
    public DateTime? DeletionTime { get; }
}

public sealed class Role : Entity<long>, IAggregateRoot,IHasSoftDelete
{
    private Role()
    {
        
    }

    public Role(string name)
    {
        Name = name;
    }

    public string Name { get; init; }= default!;

    
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
        // modelBuilder.AddIsDeletedField(); 这里做法，会不会影响性能？？？？
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
        
        // builder.ConfigureByConvention();
        builder.ToTable("User");
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(50);
        
        // builder.ConfigureByConvention();
        builder.ToTable("Role");
    }
}