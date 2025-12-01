using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
internal class ModuleApplicationBase : IModuleApplication
{
    // Cache compiled constructors to avoid repeated Expression.Compile() overhead
    private static readonly ConcurrentDictionary<Type, Func<object>> ConstructorCache = new();
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
        source.AddRange(types.Select(type => CreateModule(type, context)).OfType<IAppModule>());
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
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static IAppModule? CreateModule(Type moduleType, ConfigureServicesContext context)
    {
        var factory = ConstructorCache.GetOrAdd(moduleType, static t =>
        {
            var ctor = t.GetConstructor(Type.EmptyTypes);
            return ctor is null
                       ? throw new InvalidOperationException($"Type {t.FullName} does not have a parameterless constructor.")
                       // Use compiled delegate for faster instantiation
                       : Expression.Lambda<Func<object>>(Expression.New(ctor)).Compile();
        });
        var module = factory() as IAppModule;
        ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
        return module.GetEnable(context) ? module : null;
    }

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules</para>
    ///     <para xml:lang="zh">初始化所有模块</para>
    /// </summary>
    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider);
        var ctx = new ApplicationContext(ServiceProvider);
        foreach (var cfg in Modules)
        {
            cfg.ApplicationInitialization(ctx);
        }
    }
}