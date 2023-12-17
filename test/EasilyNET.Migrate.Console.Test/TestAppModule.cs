using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.EntityFrameworkCore.Extensions;
using EasilyNET.EntityFrameworkCore.Migrations;
using EasilyNET.Migrate.Console.Test.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable ClassNeverInstantiated.Global

/// <inheritdoc />
public sealed class TestAppModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Trace));
        //services.AddDbContext<TestDbContext>(a => a.UseSqlite("Data Source=My.db"));
        services.AddEFCore<TestDbContext>(options => options.ConfigureDbContextBuilder =
                                                         builder =>
                                                             builder.UseSqlite("Data Source=My.db").LogTo(Console.WriteLine, LogLevel.Information));
        services.AddRepository();
        services.AddSingleton<IMigrationService, MigrationService>();
        services.AddScoped<IDbSetup, DbSetupUser>();
        //services.AddScoped<ISeedData, RoleSeedData>();
    }
}