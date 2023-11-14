using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EasilyNET.EntityFrameworkCore.Test.DbContexts;

internal class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlite("Data Source=My.db");
        return new(optionsBuilder.Options, null);
    }
}

public sealed class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions options, IServiceProvider? serviceProvider) : base(options, serviceProvider) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
    }
}