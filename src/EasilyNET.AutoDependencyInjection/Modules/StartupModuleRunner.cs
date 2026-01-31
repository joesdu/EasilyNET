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
        using var serviceProvider = Services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
        var logger = loggerFactory?.CreateLogger(nameof(AutoDependencyInjection)) ?? NullLogger.Instance;
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
            // For async configuration, we synchronously wait on the returned task.
            // NOTE / 注意:
            // - ConfigureServicesAsync implementations must NOT depend on any SynchronizationContext
            //   模块的 ConfigureServicesAsync 实现不得依赖同步上下文
            // - They should use ConfigureAwait(false) on all awaited operations
            //   内部所有 await 调用应使用 ConfigureAwait(false)
            // - We synchronously wait here to keep the registration pipeline deterministic
            //   这里采用同步等待以保持注册管线的确定性
            var asyncTask = module.ConfigureServicesAsync(context);
            if (!asyncTask.IsCompleted)
            {
                asyncTask.GetAwaiter().GetResult();
            }
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Completed service configuration for {Count} modules", Modules.Count);
        }
    }
}