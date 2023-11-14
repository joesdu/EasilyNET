// See https://aka.ms/new-console-template for more information

using EasilyNET.EntityFrameworkCore.Extensions;
using EasilyNET.EntityFrameworkCore.Migrations;
using EasilyNET.EntityFrameworkCore.Test.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

IServiceCollection services = new ServiceCollection();
services.AddLogging(builder => { builder.AddConsole().SetMinimumLevel(LogLevel.Trace); });
services.AddEFCore<TestDbContext>(options =>
{
    options.ConfigureDbContextBuilder =
        builder =>
            builder.UseSqlite("Data Source=My.db").LogTo(Console.WriteLine, LogLevel.Information);
});
services.AddSingleton<IMigrationService, MigrationService>();
var serviceProvider = services.BuildServiceProvider();
var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var migrationService = serviceProvider.GetService<IMigrationService>();
var dir = AppDomain.CurrentDomain.BaseDirectory.Split(Path.DirectorySeparatorChar);
var slice = new ArraySegment<string>(dir, 0, dir.Length - 5);
var path = Path.Combine(slice.ToArray());
var rootPath = Path.Combine(path, "EasilyNET.Migrate.Console.Test.Model");
var testDbContext = serviceProvider.GetService<TestDbContext>()!;
var name = "Init_123";
var isMigration = testDbContext.Database.GetAppliedMigrations().Contains(name);

//暂时不更新，因为8.0问题
//migrationService?.InstallEfTool();
//migrationService?.UpdateEfTool();
try
{
    if (!isMigration)
    {
        migrationService?.AddMigration(name, rootPath);
    }
    migrationService?.UpdateDatabase(rootPath);
}
catch (Exception e)
{
    logger.LogError(e.Message);
}
Console.ReadKey();