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
    /// <summary>
    /// 启动模块类型
    /// </summary>
    private readonly Type _startModuleType;

    protected ModuleApplicationBase(Type? startModuleType, IServiceCollection? services)
    {
        ArgumentNullException.ThrowIfNull(startModuleType, nameof(startModuleType));
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        _startModuleType = startModuleType;
        Services = services;
        ServiceProvider = services.BuildServiceProvider();
        GetModules();
    }

    /// <summary>
    /// IServiceCollection
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// IServiceProvider?
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 模块接口
    /// </summary>
    public IList<IAppModule> Modules { get; } = [];

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposableServiceProvider) disposableServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取所有需要加载的模块
    /// </summary>
    /// <returns></returns>
    private void GetModules()
    {
        var sources = GetAllEnabledModule();
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
    /// 获取所有启用的模块
    /// </summary>
    /// <returns></returns>
    private ConcurrentBag<IAppModule> GetAllEnabledModule()
    {
        var types = AssemblyHelper.AllTypes.Where(AppModule.IsAppModule);
        var source = new ConcurrentBag<IAppModule>();
        Parallel.ForEach(types, type =>
        {
            var module = CreateModule(type);
            if (module is not null)
            {
                source.Add(module);
            }
        });
        return source;
    }

    /// <summary>
    /// 创建模块
    /// </summary>
    /// <param name="moduleType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    private IAppModule? CreateModule(Type moduleType)
    {
        var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule;
        ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
        if (module.GetEnable(new(Services, ServiceProvider))) return module;
#if DEBUG
        Console.Error.WriteLine($"{moduleType.Name} is disabled");
#endif
        return null;
    }

    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider, nameof(ServiceProvider));
        var ctx = new ApplicationContext(ServiceProvider);
        foreach (var cfg in Modules) cfg.ApplicationInitialization(ctx);
    }
}