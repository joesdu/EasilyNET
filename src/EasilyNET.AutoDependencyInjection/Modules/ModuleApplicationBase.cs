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
    /// 构造函数
    /// </summary>
    /// <param name="startupModuleType"></param>
    /// <param name="services"></param>
    protected ModuleApplicationBase(Type? startupModuleType, IServiceCollection? services)
    {
        ArgumentNullException.ThrowIfNull(startupModuleType, nameof(startupModuleType));
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ServiceProvider = null;
        StartupModuleType = startupModuleType;
        Services = services;
        GetAllEnabledModule();
        LoadModules();
    }

    /// <summary>
    /// 启动模块类型
    /// </summary>
    public Type StartupModuleType { get; }

    /// <summary>
    /// IServiceCollection
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// IServiceProvider?
    /// </summary>
    public IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// 模块接口容器
    /// </summary>
    public IList<IAppModule> Modules { get; } = [];

    /// <summary>
    /// Source
    /// </summary>
    public ConcurrentBag<IAppModule> Source { get; } = [];

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
    private void LoadModules()
    {
        var module = Source.FirstOrDefault(o => o.GetType() == StartupModuleType) ?? throw new($"类型为“{StartupModuleType.FullName}”的模块实例无法找到");
        Modules.Add(module);
        var depends = module.GetDependedTypes();
        foreach (var dependType in depends)
        {
            var dependModule = Source.FirstOrDefault(m => m.GetType() == dependType);
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
    private void GetAllEnabledModule()
    {
        var types = AssemblyHelper.AllTypes.Where(AppModule.IsAppModule);
        Parallel.ForEach(types, type =>
        {
            var module = CreateModule(type);
            if (module is not null)
            {
                Source.Add(module);
            }
        });
    }

    /// <summary>
    /// 设置 <see cref="ServiceProvider" />
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected void SetServiceProvider(IServiceProvider serviceProvider) => ServiceProvider = serviceProvider;

    /// <summary>
    /// 创建模块
    /// </summary>
    /// <param name="moduleType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    private static IAppModule? CreateModule(Type moduleType)
    {
        var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule;
        ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
        if (module.Enable) return module;
#if DEBUG
        Console.Error.WriteLine($"{moduleType.Name} is disabled");
#endif
        return null;
    }

    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider, nameof(ServiceProvider));
        using var scope = ServiceProvider.CreateScope();
        var ctx = new ApplicationContext(scope.ServiceProvider);
        foreach (var cfg in Modules) cfg.ApplicationInitialization(ctx);
    }
}