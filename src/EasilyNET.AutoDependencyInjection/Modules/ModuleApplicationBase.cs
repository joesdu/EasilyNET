using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        ServiceProvider = services.BuildServiceProvider();
        _logger = ServiceProvider.GetAutoDILogger();
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
    ///     <para xml:lang="en">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     <para xml:lang="en">Module interfaces</para>
    ///     <para xml:lang="zh">模块接口</para>
    /// </summary>
    public IList<IAppModule> Modules { get; } = [];

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposableServiceProvider)
        {
            disposableServiceProvider.Dispose();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Get all modules that need to be loaded</para>
    ///     <para xml:lang="zh">获取所有需要加载的模块</para>
    /// </summary>
    private void LoadModules()
    {
        var sources = GetAllEnabledModule();
        var module = sources.FirstOrDefault(o => o.GetType() == _startModuleType) ?? throw new($"类型为 '{_startModuleType.FullName}' 的模块实例无法找到");
        Modules.Add(module);
        var depends = module.GetDependedTypes();
        foreach (var dependType in depends)
        {
            var dependModule = sources.FirstOrDefault(m => m.GetType() == dependType);
            if (dependModule is not null && !Modules.Contains(dependModule))
            {
                Modules.Add(dependModule);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Get all enabled modules</para>
    ///     <para xml:lang="zh">获取所有启用的模块</para>
    /// </summary>
    private List<IAppModule> GetAllEnabledModule()
    {
        var types = AssemblyHelper.AllTypes.Where(AppModule.IsAppModule).ToArray();
        var context = new ConfigureServicesContext(Services, ServiceProvider);
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
                           ? throw new InvalidOperationException($"Type {t.FullName} does not have a parameterless constructor.")
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
    ///     <para xml:lang="en">Initialize all modules</para>
    ///     <para xml:lang="zh">初始化所有模块</para>
    /// </summary>
    // TODO?: [Obsolete("Use InitializeModulesAsync instead")]
    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting synchronous initialization of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(ServiceProvider);
        foreach (var cfg in Modules)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Initializing module: {ModuleType}", cfg.GetType().Name);
            }
            // 同步等待模块初始化完成，确保初始化顺序正确
            cfg.ApplicationInitialization(ctx).GetAwaiter().GetResult();
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
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    protected async Task InitializeModulesAsync(CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider);
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting async initialization of {Count} modules", Modules.Count);
        }
        var ctx = new ApplicationContext(ServiceProvider);
        foreach (var cfg in Modules)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Initializing module: {ModuleType}", cfg.GetType().Name);
            }
            await cfg.ApplicationInitialization(ctx).ConfigureAwait(false);
        }
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Completed async initialization of all modules");
        }
    }
}