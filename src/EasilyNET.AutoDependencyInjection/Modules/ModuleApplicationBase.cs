using System.Collections.Concurrent;
using System.Linq.Expressions;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
internal class ModuleApplicationBase : IModuleApplication
{
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
        ArgumentNullException.ThrowIfNull(startModuleType, nameof(startModuleType));
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        _startModuleType = startModuleType;
        Services = services;
        ServiceProvider = services.BuildServiceProvider();
        GetModules().GetAwaiter().GetResult();
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
        if (ServiceProvider is IDisposable disposableServiceProvider) disposableServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Get all modules that need to be loaded</para>
    ///     <para xml:lang="zh">获取所有需要加载的模块</para>
    /// </summary>
    private async Task GetModules()
    {
        var sources = await GetAllEnabledModule();
        var module = sources.FirstOrDefault(o => o.GetType() == _startModuleType) ?? throw new($"类型为“{_startModuleType.FullName}”的模块实例无法找到");
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
    private async Task<ConcurrentBag<IAppModule>> GetAllEnabledModule()
    {
        var types = AssemblyHelper.AllTypes.Where(AppModule.IsAppModule);
        var source = new ConcurrentBag<IAppModule>();
        await Parallel.ForEachAsync(types, async (type, _) =>
        {
            var module = await CreateModule(type);
            if (module is not null)
            {
                source.Add(module);
            }
        });
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
    private async Task<IAppModule?> CreateModule(Type moduleType)
    {
        return await Task.Run(() =>
        {
            var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule;
            ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
            if (module.GetEnable(new(Services, ServiceProvider))) return module;
#if DEBUG
            Console.Error.WriteLine($"Module: {moduleType.Name} is disabled");
#endif
            return null;
        });
    }

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules</para>
    ///     <para xml:lang="zh">初始化所有模块</para>
    /// </summary>
    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider, nameof(ServiceProvider));
        var ctx = new ApplicationContext(ServiceProvider);
        foreach (var cfg in Modules) cfg.ApplicationInitialization(ctx);
    }
}