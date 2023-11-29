// See https://aka.ms/new-console-template for more information

using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.EntityFrameworkCore.Extensions;
using EasilyNET.EntityFrameworkCore.Migrations;
using EasilyNET.EntityFrameworkCore.Test.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using (var application = ApplicationFactory.Create<TestAppModule>())
{
    var serviceProvider = application.ServiceProvider!;
    var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
    var migrationService = serviceProvider.GetService<IMigrationService>();
    var dir = AppDomain.CurrentDomain.BaseDirectory.Split(Path.DirectorySeparatorChar);
    var slice = new ArraySegment<string>(dir, 0, dir.Length - 5);
    var path = Path.Combine(slice.ToArray());
    var rootPath = Path.Combine(path, "EasilyNET.Migrate.Console.Test.Model");
    var testDbContext = serviceProvider.GetService<TestDbContext>()!;
    const string name = "Init_123";
    var isMigration = testDbContext.Database.GetPendingMigrations().Where(o => o.Contains(name))!.Any();

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
}
Console.ReadKey();

public sealed class TestAppModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        services.AddLogging(builder => { builder.AddConsole().SetMinimumLevel(LogLevel.Trace); });
        services.AddEFCore<TestDbContext>(options =>
        {
            options.ConfigureDbContextBuilder =
                builder =>
                    builder.UseSqlite("Data Source=My.db").LogTo(Console.WriteLine, LogLevel.Information);
        });
        services.AddSingleton<IMigrationService, MigrationService>();
    }
}