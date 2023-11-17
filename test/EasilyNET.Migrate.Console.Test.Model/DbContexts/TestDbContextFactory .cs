using EasilyNET.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;
using System;

namespace EasilyNET.EntityFrameworkCore.Test.DbContexts;

internal class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlite("Data Source=My.db").LogTo(Console.WriteLine, LogLevel.Information);
        return new(optionsBuilder.Options, null);
    }
}

public sealed class TestDbContext : DefaultDbContext
{
    public TestDbContext(DbContextOptions options, IServiceProvider? serviceProvider) : base(options, serviceProvider) { }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, RemoveForeignKeyMigrationsSqlGenerator>();
        optionsBuilder.ReplaceService<IMigrationsModelDiffer, MigrationsModelDifferWithoutForeignKey>();
        base.OnConfiguring(optionsBuilder);
    }
}