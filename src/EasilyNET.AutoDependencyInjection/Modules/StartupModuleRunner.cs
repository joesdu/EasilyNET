using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc cref="IStartupModuleRunner" />
internal sealed class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    /// <inheritdoc />
    internal StartupModuleRunner(Type startupModuleType, IServiceCollection services) : base(startupModuleType, services)
    {
        services.AddSingleton<IStartupModuleRunner>(this);
        ConfigureServices();
    }

    private IServiceScope? ServiceScope { get; set; }

    /// <inheritdoc />
    public void Initialize(IServiceProvider? provider = null)
    {
        if (provider is not null)
        {
            SetServiceProvider(provider);
        }
        else
        {
            ServiceScope = Services.BuildServiceProvider().CreateScope();
            SetServiceProvider(ServiceScope.ServiceProvider);
        }
        InitializeModules();
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        base.Dispose();
        ServiceScope?.Dispose();
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private void ConfigureServices()
    {
        var context = new ConfigureServicesContext(Services, ServiceProvider);
        Services.AddSingleton(context);
        foreach (var config in Modules)
        {
            Services.AddSingleton(config);
            config.ConfigureServices(context);
        }
    }
}