using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
    public void Initialize(IServiceProvider serviceProvider) => InitializeModules(serviceProvider);

    /// <inheritdoc />
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) => InitializeModulesAsync(serviceProvider, cancellationToken);

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
            return _instance ??= new(startModuleType, services);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Configure services for all modules. This is called synchronously during service registration.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     为所有模块配置服务。这在服务注册期间同步调用。
    ///     </para>
    /// </summary>
    private void ConfigureServices()
    {
        var context = new ConfigureServicesContext(Services);
        Services.AddSingleton(context);
        var logger = Services.BuildServiceProvider().GetService<ILoggerFactory>()
                             ?.CreateLogger(nameof(AutoDependencyInjection)) ??
                     NullLogger.Instance;
        foreach (var module in Modules)
        {
            Services.AddSingleton(module);
            var moduleType = module.GetType();
            if (logger.IsEnabled(LogLevel.Trace))
            {
                logger.LogTrace("Configuring services for module: {ModuleType}", moduleType.Name);
            }
            // Call synchronous ConfigureServices - this is safe and won't deadlock
            module.ConfigureServices(context);
            // For async configuration, we run it on a thread pool thread to avoid deadlocks
            // This is necessary because we're in a synchronous context (service registration)
            var asyncTask = module.ConfigureServicesAsync(context);
            if (!asyncTask.IsCompleted)
            {
                // Use Task.Run to avoid deadlocks with synchronization contexts
                Task.Run(() => asyncTask).GetAwaiter().GetResult();
            }
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Completed service configuration for {Count} modules", Modules.Count);
        }
    }
}