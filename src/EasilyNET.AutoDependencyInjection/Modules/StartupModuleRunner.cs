using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc cref="IStartupModuleRunner" />
internal sealed class StartupModuleRunner : ModuleApplicationBase, IStartupModuleRunner
{
    private StartupModuleRunner(Type startModuleType, IServiceCollection services) : base(startModuleType, services)
    {
        Services.AddSingleton<IStartupModuleRunner>(this);
        ConfigureServices();
    }

    /// <inheritdoc />
    public void Initialize(IServiceProvider serviceProvider) => InitializeModules(serviceProvider);

    /// <inheritdoc />
    public Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) => InitializeModulesAsync(serviceProvider, cancellationToken);

    /// <inheritdoc />
    public void Shutdown(IServiceProvider serviceProvider) => ShutdownModules(serviceProvider);

    /// <inheritdoc />
    public Task ShutdownAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) => ShutdownModulesAsync(serviceProvider, cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Create a new instance of <see cref="StartupModuleRunner" /></para>
    ///     <para xml:lang="zh">创建 <see cref="StartupModuleRunner" /> 的新实例</para>
    /// </summary>
    /// <param name="startModuleType">
    ///     <para xml:lang="en">The type of the startup module</para>
    ///     <para xml:lang="zh">启动模块的类型</para>
    /// </param>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    internal static StartupModuleRunner Create(Type startModuleType, IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(startModuleType);
        ArgumentNullException.ThrowIfNull(services);
        return new(startModuleType, services);
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
        var configuration = ConfigureServicesContext.ExtractConfiguration(Services);
        var environment = ConfigureServicesContext.ExtractEnvironment(Services);
        var context = new ConfigureServicesContext(Services, configuration, environment);
        var logger = GetBootstrapLogger(Services);
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

    /// <summary>
    ///     <para xml:lang="en">Get a bootstrap logger without building a full ServiceProvider</para>
    ///     <para xml:lang="zh">获取引导日志记录器，无需构建完整的 ServiceProvider</para>
    /// </summary>
    private static ILogger GetBootstrapLogger(IServiceCollection services)
    {
        // Try to get an existing ILoggerFactory from the services without building a provider
        var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        return loggerFactoryDescriptor?.ImplementationInstance is ILoggerFactory existingFactory
                   ? existingFactory.CreateLogger(nameof(AutoDependencyInjection))
                   : NullLogger.Instance;
    }
}