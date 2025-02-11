using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc cref="IStartupModuleRunner" />
internal sealed class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    private static readonly Lazy<StartupModuleRunner> _instance = new(() => new(_startModuleType, _services));

    private static Type? _startModuleType;
    private static IServiceCollection? _services;

    private StartupModuleRunner(Type? startModuleType, IServiceCollection? services) : base(startModuleType, services)
    {
        Services.AddSingleton<IStartupModuleRunner>(this);
        ConfigureServices();
    }

    public void Initialize() => InitializeModules();

    internal static StartupModuleRunner Instance(Type startModuleType, IServiceCollection services)
    {
        if (_instance.IsValueCreated)
        {
            return _instance.Value;
        }
        Interlocked.CompareExchange(ref _startModuleType, startModuleType ?? throw new ArgumentNullException(nameof(startModuleType)), null);
        Interlocked.CompareExchange(ref _services, services ?? throw new ArgumentNullException(nameof(services)), null);
        return _instance.Value;
    }

    private void ConfigureServices()
    {
        var context = new ConfigureServicesContext(Services, ServiceProvider);
        Services.AddSingleton(context);
        foreach (var module in Modules)
        {
            Services.AddSingleton(module);
            module.ConfigureServices(context);
        }
    }
}