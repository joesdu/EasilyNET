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

    /// <inheritdoc />
    public void Initialize() => InitializeModules();

    /// <summary>
    /// 配置服务
    /// </summary>
    private void ConfigureServices()
    {
        if (ServiceProvider is null) SetServiceProvider(Services.BuildServiceProvider());
        var context = new ConfigureServicesContext(Services, ServiceProvider);
        Services.AddSingleton(context);
        foreach (var config in Modules)
        {
            Services.AddSingleton(config);
            config.ConfigureServices(context);
        }
    }
}