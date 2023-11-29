using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <summary>
/// 启动模块运行器
/// </summary>
internal class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    /// <summary>
    /// 程序启动运行时
    /// </summary>
    /// <param name="startupModuleType"></param>
    /// <param name="services"></param>
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

    /// <summary>
    /// Dispose
    /// </summary>
    public new void Dispose()
    {
        base.Dispose();
        if (ServiceProvider is IDisposable disposableServiceProvider) disposableServiceProvider.Dispose();
        ServiceScope?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 配置服务
    /// </summary>
    private void ConfigureServices()
    {
        var context = new ConfigureServicesContext(Services);
        Services.AddSingleton(context);
        foreach (var config in Modules)
        {
            Services.AddSingleton(config);
            config.ConfigureServices(context);
        }
    }
}