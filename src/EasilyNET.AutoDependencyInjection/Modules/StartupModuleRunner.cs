using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc cref="IStartupModuleRunner" />
internal sealed class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    private static readonly Lazy<StartupModuleRunner> _instance = new(() => new(_startupModuleType, _services));

    private static Type? _startupModuleType;
    private static IServiceCollection? _services;

    private StartupModuleRunner(Type? startupModuleType, IServiceCollection? services) : base(startupModuleType, services)
    {
        Services.AddSingleton<IStartupModuleRunner>(this);
        ConfigureServices();
    }

    public void Initialize() => InitializeModules();

    internal static StartupModuleRunner Instance(Type startupModuleType, IServiceCollection services)
    {
        if (_instance.IsValueCreated)
        {
            return _instance.Value;
        }
        Interlocked.CompareExchange(ref _startupModuleType, startupModuleType ?? throw new ArgumentNullException(nameof(startupModuleType)), null);
        Interlocked.CompareExchange(ref _services, services ?? throw new ArgumentNullException(nameof(services)), null);
        return _instance.Value;
    }

    private void ConfigureServices()
    {
        if (ServiceProvider is null) SetServiceProvider(Services.BuildServiceProvider());
        var context = new ConfigureServicesContext(Services, ServiceProvider);
        Services.AddSingleton(context);
        foreach (var module in Modules)
        {
            Services.AddSingleton(module);
            module.ConfigureServices(context);
        }
    }
}