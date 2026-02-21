using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
internal class ModuleApplicationBase : IModuleApplication
{
    // Cache compiled constructors to avoid repeated Expression.Compile() overhead
    private static readonly ConcurrentDictionary<Type, Func<object>> ConstructorCache = new();
    private readonly ILogger _logger;
    private readonly Type _startModuleType;

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    /// <param name="startModuleType">
    ///     <para xml:lang="en">Type of the start module</para>
    ///     <para xml:lang="zh">启动模块的类型</para>
    /// </param>
    /// <param name="services">
    ///     <para xml:lang="en"><see cref="IServiceCollection" /> to configure services</para>
    ///     <para xml:lang="zh">用于配置服务的 <see cref="IServiceCollection" /></para>
    /// </param>
    protected ModuleApplicationBase(Type? startModuleType, IServiceCollection? services)
    {
        ArgumentNullException.ThrowIfNull(startModuleType);
        ArgumentNullException.ThrowIfNull(services);
        _startModuleType = startModuleType;
        Services = services;
        // Get bootstrap logger without building a ServiceProvider
        _logger = CreateBootstrapLogger(services);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Initializing module application with startup module: {ModuleType}", startModuleType.Name);
        }
        LoadModules();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    ///     <para xml:lang="en">Module interfaces - ordered so that dependencies come BEFORE dependents</para>
    ///     <para xml:lang="zh">模块接口 - 按顺序排列，依赖项在被依赖项之前</para>
    /// </summary>
    public IList<IAppModule> Modules { get; } = [];

    /// <inheritdoc />
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Create a bootstrap logger without building a full ServiceProvider</para>
    ///     <para xml:lang="zh">创建引导日志记录器，无需构建完整的 ServiceProvider</para>
    /// </summary>
    private static ILogger CreateBootstrapLogger(IServiceCollection services)
    {
        // Try to get an existing ILoggerFactory from the services without building a provider
        var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (loggerFactoryDescriptor?.ImplementationInstance is ILoggerFactory existingFactory)
        {
            return existingFactory.CreateLogger(nameof(AutoDependencyInjection));
        }
        // If no ILoggerFactory instance is available, use NullLogger to avoid building a temporary ServiceProvider
        return NullLogger.Instance;
    }

    /// <summary>
    ///     <para xml:lang="en">Load modules by recursively resolving from the root module's DependsOn declarations</para>
    ///     <para xml:lang="zh">通过递归解析根模块的 DependsOn 声明来加载模块</para>
    /// </summary>
    private void LoadModules()
    {
        var configuration = ConfigureServicesContext.ExtractConfiguration(Services);
        var context = new ConfigureServicesContext(Services, configuration);
        // Create the start module and collect all referenced module types from DependsOn
        var startModule = CreateModuleOrThrow(_startModuleType);
        // Get dependencies in topological order (dependencies first)
        var dependencyTypes = startModule.GetDependedTypes(_startModuleType).ToList();
        // Create and add dependency modules in order
        foreach (var depModule in dependencyTypes.Select(depType => CreateModule(depType, context, _logger)).OfType<IAppModule>().Where(depModule => !Modules.Contains(depModule)))
        {
            Modules.Add(depModule);
        }
        // Check if start module is enabled, then add it last
        if (startModule.GetEnable(context))
        {
            Modules.Add(startModule);
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Module execution order: {Order}",
                string.Join(" -> ", Modules.Select(m => m.GetType().Name)));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Create a module instance or throw if creation fails</para>
    ///     <para xml:lang="zh">创建模块实例，如果创建失败则抛出异常</para>
    /// </summary>
    private static IAppModule CreateModuleOrThrow(Type moduleType)
    {
        var factory = ConstructorCache.GetOrAdd(moduleType, static t =>
        {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            return ctor is null
                       ? throw new InvalidOperationException($"Type '{t.FullName}' does not have a parameterless constructor.")
                       : Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile();
        });
        return factory() as IAppModule ?? throw new InvalidOperationException($"Type '{moduleType.FullName}' does not implement IAppModule.");
    }

    /// <summary>
    ///     <para xml:lang="en">Create a module instance with enable check</para>
    ///     <para xml:lang="zh">创建模块实例并检查是否启用</para>
    /// </summary>
    /// <param name="moduleType">
    ///     <para xml:lang="en">Type of the module</para>
    ///     <para xml:lang="zh">模块的类型</para>
    /// </param>
    /// <param name="context">
    ///     <para xml:lang="en">Configure services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    /// <param name="logger">
    ///     <para xml:lang="en">Logger instance</para>
    ///     <para xml:lang="zh">日志记录器实例</para>
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IAppModule? CreateModule(Type moduleType, ConfigureServicesContext context, ILogger logger)
    {
        try
        {
            var module = CreateModuleOrThrow(moduleType);
            if (module.GetEnable(context))
            {
                return module;
            }
            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("Module {ModuleType} is disabled", moduleType.Name);
            }
            return null;
        }
        catch (Exception ex)
        {
            // Log the error but don't break other modules
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "Failed to create module {ModuleType}", moduleType.FullName);
            }
            return null;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules synchronously</para>
    ///     <para xml:lang="zh">同步初始化所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The built service provider</para>
    ///     <para xml:lang="zh">已构建的服务提供者</para>
    /// </param>
    protected void InitializeModules(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting synchronous initialization of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(serviceProvider);
        foreach (var module in Modules)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Initializing module: {ModuleType}", module.GetType().Name);
            }
            module.ApplicationInitializationSync(ctx);
            // For modules that only override the async method, run it synchronously
            var asyncTask = module.ApplicationInitialization(ctx);
            if (!asyncTask.IsCompleted)
            {
                asyncTask.GetAwaiter().GetResult();
            }
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed initialization of all modules");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules asynchronously</para>
    ///     <para xml:lang="zh">异步初始化所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The built service provider</para>
    ///     <para xml:lang="zh">已构建的服务提供者</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    protected async Task InitializeModulesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting async initialization of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(serviceProvider);
        foreach (var module in Modules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Initializing module: {ModuleType}", module.GetType().Name);
            }
            module.ApplicationInitializationSync(ctx);
            await module.ApplicationInitialization(ctx).ConfigureAwait(false);
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed async initialization of all modules");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Shutdown all modules in reverse order</para>
    ///     <para xml:lang="zh">按逆序关闭所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    protected void ShutdownModules(IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting shutdown of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(serviceProvider);
        // Shutdown in reverse order (dependents before dependencies)
        for (var i = Modules.Count - 1; i >= 0; i--)
        {
            var module = Modules[i];
            try
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Shutting down module: {ModuleType}", module.GetType().Name);
                }
                var task = module.ApplicationShutdown(ctx);
                if (!task.IsCompleted)
                {
                    task.GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Error shutting down module {ModuleType}", module.GetType().Name);
                }
            }
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed shutdown of all modules");
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Shutdown all modules in reverse order asynchronously</para>
    ///     <para xml:lang="zh">异步按逆序关闭所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    protected async Task ShutdownModulesAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting async shutdown of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(serviceProvider);
        // Shutdown in reverse order (dependents before dependencies)
        for (var i = Modules.Count - 1; i >= 0; i--)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var module = Modules[i];
            try
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace("Shutting down module: {ModuleType}", module.GetType().Name);
                }
                await module.ApplicationShutdown(ctx).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "Error shutting down module {ModuleType}", module.GetType().Name);
                }
            }
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed async shutdown of all modules");
        }
    }
}