using EasilyNET.EntityFrameworkCore;
using EasilyNET.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Reflection;

namespace EasilyNET.Migrate.Console.Test.Model;

internal class TestDbContextFactory : IDesignTimeDbContextFactory<TestDbContext>
{
    public TestDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TestDbContext>();
        optionsBuilder.UseSqlite("Data Source=My.db");
        return new(optionsBuilder.Options, null);
    }
}

public sealed class TestDbContext(DbContextOptions options, IServiceProvider? serviceProvider) : DefaultDbContext(options, serviceProvider)
{
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ReplaceService<IMigrationsSqlGenerator, RemoveForeignKeyMigrationsSqlGenerator>();
        optionsBuilder.ReplaceService<IMigrationsModelDiffer, MigrationsModelDifferWithoutForeignKey>();
        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
    }

    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}