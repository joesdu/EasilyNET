using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Core.BaseType;
using EasilyNET.Core.Domains;
using EasilyNET.EntityFrameworkCore.Extensions;
using EasilyNET.EntityFrameworkCore.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.EntityFrameworkCore.Test;

/// <summary>
/// </summary>
internal sealed class TestAppModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddEFCore<TestDbContext>(options => options.ConfigureDbContextBuilder =
                                                                 builder =>
                                                                     builder.UseSqlite("Data Source=My.db", o => o.MigrationsAssembly(nameof(RepositoryTests))).LogTo(Console.WriteLine, LogLevel.Information));
        context.Services.AddScoped<IUserRepository, UserRepository>();
        context.Services.AddScoped(typeof(IRepository<,>), typeof(Repository<,>));
        context.Services.AddSingleton(SnowFlakeId.Default);
        context.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
    }
}