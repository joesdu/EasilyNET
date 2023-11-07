using EasilyNET.Core.BaseType;
using EasilyNET.Core.Domains;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EntityFrameworkCore.Test.Unit;

public sealed class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options) { }

    private static string NextId => SnowFlakeId.Default.NextId().ToString();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new RoleConfiguration());
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableDetailedErrors();
        optionsBuilder.EnableSensitiveDataLogging();
    }
    //protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    //{
    //    modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    //}

    //只是测试时候使用
    /// <inheritdoc />
    //protected override string GetUserId() => NextId;
}

public sealed class Role : Entity<long>, IAggregateRoot
{
    public Role() { }

    public Role(long id, string name)
    {
        Id = id;
        Name = name;
    }

    public string Name { get; set; } = default!;

    public string FirstName { get; set; }

    public byte[] Version { get; set; } = default!;
}

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).IsRequired().HasMaxLength(50);
        builder.Property(o => o.FirstName).HasMaxLength(50);
        builder.ToTable("Role");
    }
}