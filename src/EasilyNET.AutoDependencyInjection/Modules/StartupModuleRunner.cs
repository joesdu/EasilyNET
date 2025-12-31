using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc cref="IStartupModuleRunner" />
internal sealed class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    private static readonly Lock _lock = new();
    private static StartupModuleRunner? _instance;

    private StartupModuleRunner(Type startModuleType, IServiceCollection services) : base(startModuleType, services)
    {
        Services.AddSingleton<IStartupModuleRunner>(this);
        ConfigureServices();
    }

    /// <inheritdoc />
    // TODO?: [Obsolete("Use InitializeAsync method instead.")]
    public void Initialize() => InitializeModules();

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default) => InitializeModulesAsync(cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Get or create the singleton instance of <see cref="StartupModuleRunner" /></para>
    ///     <para xml:lang="zh">获取或创建 <see cref="StartupModuleRunner" /> 的单例实例</para>
    /// </summary>
    /// <param name="startModuleType">
    ///     <para xml:lang="en">The type of the startup module</para>
    ///     <para xml:lang="zh">启动模块的类型</para>
    /// </param>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    internal static StartupModuleRunner Instance(Type startModuleType, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(startModuleType);
        ArgumentNullException.ThrowIfNull(services);
        lock (_lock)
        {
            if (_instance is not null)
            {
                return _instance;
            }
        }
        lock (_lock)
        {
            return _instance ??= new(startModuleType, services);
        }
    }

    private void ConfigureServices()
    {
        var context = new ConfigureServicesContext(Services, ServiceProvider);
        Services.AddSingleton(context);
        foreach (var module in Modules)
        {
            Services.AddSingleton(module);
            // ConfigureServices 返回 Task，需要同步等待以确保服务按顺序注册
            // 注意：这里保持同步调用是因为服务注册必须在 BuildServiceProvider 之前完成
            module.ConfigureServices(context).GetAwaiter().GetResult();
        }
    }
}