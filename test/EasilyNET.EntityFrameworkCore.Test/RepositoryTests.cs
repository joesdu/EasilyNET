namespace EasilyNET.EntityFrameworkCore.Test;

[TestClass]
public class RepositoryTests
{
    [TestMethod]
    public async Task AddUserAsync_ShouldAddUserToDatabase()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        application.Initialize();
        var userRepository = application.ServiceProvider!.GetRequiredService<IUserRepository>();
        for (var i = 0; i < 10; i++)
        {
            var user = new User($"大黄瓜_{i}", 18);
            await userRepository.AddAsync(user);
        }
        // Act
        var re = await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        Assert.IsTrue(re > 0);
        // Arrange
    }

    [TestMethod]
    public async Task UpdateUserAsync_ShouldUpdateUserToDatabase()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        // Arrange
        var userRepository = application.ServiceProvider!.GetRequiredService<IUserRepository>();
        // Act
        var user = await userRepository.FindEntity.FirstOrDefaultAsync();
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
        using var application = ApplicationFactory.Create<TestAppModule>();
        // Arrange
        var userRepository = application.ServiceProvider!.GetRequiredService<IUserRepository>();
        // Act
        var user = await userRepository.FindEntity.FirstOrDefaultAsync();
        userRepository.Remove(user!);
        var count = await userRepository.UnitOfWork.SaveChangesAsync();
        // Assert
        Assert.IsTrue(count == 1);
    }

    /// <summary>
    /// 添加角色
    /// </summary>
    [TestMethod]
    public async Task AddRoleAsync_ShouldAddRoleToDatabase()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        // Arrange
        var snowFlakeId = application.ServiceProvider!.GetService<ISnowFlakeId>();
        var roleRepository = application.ServiceProvider!.GetService<IRepository<Role, long>>();
        for (var i = 0; i < 10; i++)
        {
            var role = new Role(snowFlakeId!.NextId(), $"大黄瓜_{i}");
            await roleRepository!.AddAsync(role);
        }
        // Act
        var count = await roleRepository!.UnitOfWork.SaveChangesAsync();
        // Assert
        Assert.IsTrue(count > 0);
    }

    /// <summary>
    /// 命令添加用户
    /// </summary>
    [TestMethod]
    public async Task AddUserAsync_ShouldCommand()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        var addUserCommand = new AddUserCommand(new("Command", 200));
        var sender = application.ServiceProvider?.GetService<ISender>();
        var count = await sender!.Send(addUserCommand);
        Assert.IsTrue(count > 0);
    }

    /// <summary>
    /// 查询用户
    /// </summary>
    [TestMethod]
    public async Task UserListQuery_ShouldUserList()
    {
        using var application = ApplicationFactory.Create<TestAppModule>();
        var query = new UserListQuery();
        var sender = application.ServiceProvider?.GetService<ISender>();
        var reulst = await sender!.Send(query);
        Assert.IsTrue(reulst.Count > 0);
    }
}

public sealed class User : Entity<long>, IAggregateRoot, IMayHaveCreator<long?>, IHasCreationTime, IHasModifierId<long?>, IHasModificationTime, IHasDeleterId<long?>, IHasDeletionTime, IQuery<UserListQuery>
{
    private User() { }

    public User(string name, int age)
    {
        Name = name;
        Age = age;
        AddDomainEvent(new AddUserDomainEvent(this));
    }

    public string Name { get; private set; } = default!;

    public int Age { get; }

    public byte[] Version { get; set; } = default!;

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

public sealed class Role : Entity<long>, IAggregateRoot
{
    private Role() { }

    public Role(long id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Name { get; set; } = default!;

    public byte[] Version { get; set; } = default!;
}

// ReSharper disable once PartialTypeWithSinglePart
public partial class Test : Entity<long>, IHasCreationTime, IHasModifierId<long?>, IMayHaveCreator<long?>, IHasDeleterId<long?>, IHasDeletionTime, IHasModificationTime;

public sealed class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options, IServiceProvider? serviceProvider)
        : base(options, serviceProvider)
    {
        Database.EnsureCreated();
    }

    private static string NextId => SnowFlakeId.Default.NextId().ToString();

    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    //只是测试时候使用
    /// <inheritdoc />
    protected override string GetUserId() => NextId;
}

public interface IUserRepository : IRepository<User, long> { }

/// <summary>
/// UserRepository
/// </summary>
/// <param name="dbContext"></param>
public class UserRepository(TestDbContext dbContext) : RepositoryBase<User, long, TestDbContext>(dbContext), IUserRepository { }

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).ValueGeneratedOnAdd().UseSnowFlakeValueGenerator(); //新增时使用生成雪花ID
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

/// <summary>
/// 添加用户命令
/// </summary>
internal sealed class AddUserCommand : ICommand<int>
{
    /// <summary>
    /// 添加
    /// </summary>
    /// <param name="user"></param>
    public AddUserCommand(User user)
    {
        User = user;
    }

    public User User { get; }
}

internal sealed class AddUserCommandHandler : ICommandHandler<AddUserCommand, int>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// </summary>
    /// <param name="userRepository"></param>
    public AddUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<int> Handle(AddUserCommand request, CancellationToken cancellationToken)
    {
        await _userRepository.AddAsync(request.User);
        var count = await _userRepository.UnitOfWork.SaveChangesAsync();
        return count;
    }
}

/// <summary>
/// </summary>
internal sealed class UserListQuery : IQuery<List<User>> { }

internal sealed class UserListQueryHandler : IQueryHandler<UserListQuery, List<User>>
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// </summary>
    /// <param name="userRepository"></param>
    public UserListQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <inheritdoc />
    public async Task<List<User>> Handle(UserListQuery request, CancellationToken cancellationToken) => await _userRepository.FindEntity.ToListAsync();
}