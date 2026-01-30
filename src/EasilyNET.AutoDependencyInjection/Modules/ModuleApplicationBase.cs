using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.Core.Misc;
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
        // Build a minimal ServiceProvider only for logging and configuration access
        // This provider is NOT used for resolving user services
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
    ///     <para xml:lang="en">Create a bootstrap logger for module initialization</para>
    ///     <para xml:lang="zh">为模块初始化创建引导日志记录器</para>
    /// </summary>
    private static ILogger CreateBootstrapLogger(IServiceCollection services)
    {
        // Try to get an existing ILoggerFactory from the services
        var loggerFactoryDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ILoggerFactory));
        if (loggerFactoryDescriptor?.ImplementationInstance is ILoggerFactory existingFactory)
        {
            return existingFactory.CreateLogger(nameof(AutoDependencyInjection));
        }
        // Build a temporary provider just for logging
        using var tempProvider = services.BuildServiceProvider();
        var factory = tempProvider.GetService<ILoggerFactory>();
        return factory?.CreateLogger(nameof(AutoDependencyInjection)) ?? NullLogger.Instance;
    }

    /// <summary>
    ///     <para xml:lang="en">Get all modules that need to be loaded, ordered by dependencies (dependencies first)</para>
    ///     <para xml:lang="zh">获取所有需要加载的模块，按依赖顺序排列（依赖项在前）</para>
    /// </summary>
    private void LoadModules()
    {
        var allModuleInstances = GetAllEnabledModules();
        var moduleByType = allModuleInstances.ToDictionary(m => m.GetType());
        var startModule = moduleByType.GetValueOrDefault(_startModuleType) ?? throw new InvalidOperationException($"Module instance of type '{_startModuleType.FullName}' could not be found.");
        // Get dependencies in topological order (dependencies first)
        var dependencyTypes = startModule.GetDependedTypes(_startModuleType).ToList();
        // Add dependencies first (they are already in correct order from GetDependedTypes)
        foreach (var depType in dependencyTypes)
        {
            if (moduleByType.TryGetValue(depType, out var depModule) && !Modules.Contains(depModule))
            {
                Modules.Add(depModule);
            }
        }
        // Add the startup module last (it depends on all others)
        Modules.Add(startModule);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Module execution order: {Order}",
                string.Join(" -> ", Modules.Select(m => m.GetType().Name)));
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get all enabled modules</para>
    ///     <para xml:lang="zh">获取所有启用的模块</para>
    /// </summary>
    private List<IAppModule> GetAllEnabledModules()
    {
        var types = AssemblyHelper.AllTypes.Where(AppModule.IsAppModule).ToArray();
        // Create a minimal context for GetEnable check
        // Note: This context has limited ServiceProvider capabilities
        var context = new ConfigureServicesContext(Services);
        var source = new List<IAppModule>(types.Length);
        source.AddRange(types.Select(type => CreateModule(type, context, _logger)).OfType<IAppModule>());
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Found {Count} enabled modules", source.Count);
        }
        return source;
    }

    /// <summary>
    ///     <para xml:lang="en">Create a module instance</para>
    ///     <para xml:lang="zh">创建模块实例</para>
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
            var factory = ConstructorCache.GetOrAdd(moduleType, static t =>
            {
                var ctor = t.GetConstructor(Type.EmptyTypes);
                return ctor is null
                           ? throw new InvalidOperationException($"Type '{t.FullName}' does not have a parameterless constructor.")
                           : Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile();
            });
            var module = factory() as IAppModule;
            ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
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
            // Use a dedicated thread pool thread to avoid deadlocks in sync-over-async scenarios
            // This is safer than .GetAwaiter().GetResult() which can deadlock with sync contexts
            Task.Run(() => module.ApplicationInitialization(ctx)).GetAwaiter().GetResult();
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
            await module.ApplicationInitialization(ctx).ConfigureAwait(false);
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed async initialization of all modules");
        }
    }
}