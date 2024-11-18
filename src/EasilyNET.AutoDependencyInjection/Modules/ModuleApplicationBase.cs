using System.Collections.Concurrent;
using System.Linq.Expressions;
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
        services.AddSingleton<IModuleApplication>(this);
        services.TryAddObjectAccessor<IServiceProvider>();
        GetAllEnabledModule(services);
        Source = CachedSource;
        Modules = LoadModules;
    }

    private static ConcurrentBag<IAppModule> CachedSource { get; } = [];

    /// <summary>
    /// 获取所有需要加载的模块
    /// </summary>
    /// <returns></returns>
    private IReadOnlyList<IAppModule> LoadModules
    {
        get
        {
            if (LoadedModules != null)
            {
                return LoadedModules;
            }
            List<IAppModule> modules = [];
            var module = Source.FirstOrDefault(o => o.GetType() == StartupModuleType) ?? throw new($"类型为“{StartupModuleType.FullName}”的模块实例无法找到");
            modules.Add(module);
            var depends = module.GetDependedTypes();
            foreach (var dependType in depends)
            {
                var dependModule = Source.FirstOrDefault(m => m.GetType() == dependType);
                if (dependModule is not null && !modules.Contains(dependModule))
                {
                    modules.Add(dependModule);
                }
            }
            LoadedModules = modules;
            return LoadedModules;
        }
    }

    private IReadOnlyList<IAppModule>? LoadedModules { get; set; }

    /// <summary>
    /// 启动模块类型
    /// </summary>
    public Type StartupModuleType { get; set; }

    /// <summary>
    /// IServiceCollection
    /// </summary>
    public IServiceCollection Services { get; set; }

    /// <summary>
    /// IServiceProvider?
    /// </summary>
    public IServiceProvider? ServiceProvider { get; set; }

    /// <summary>
    /// 模块接口容器
    /// </summary>
    public IReadOnlyList<IAppModule> Modules { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    public IEnumerable<IAppModule> Source { get; }

    /// <inheritdoc />
    public virtual void Dispose()
    {
        if (ServiceProvider is IDisposable disposableServiceProvider) disposableServiceProvider.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取所有启用的模块
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static void GetAllEnabledModule(IServiceCollection services)
    {
        if (!CachedSource.IsEmpty) return;
        var types = AssemblyHelper.FindTypes(AppModule.IsAppModule);
        foreach (var o in types)
        {
            var c = CreateModule(services, o);
            if (c is not null)
            {
                CachedSource.Add(c);
            }
        }
    }

    /// <summary>
    /// 设置 <see cref="ServiceProvider" />
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        ServiceProvider.GetRequiredService<IObjectAccessor<IServiceProvider>>().Value = ServiceProvider;
    }

    /// <summary>
    /// 创建模块
    /// </summary>
    /// <param name="services"></param>
    /// <param name="moduleType"></param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <returns></returns>
    private static IAppModule? CreateModule(IServiceCollection services, Type moduleType)
    {
        var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule;
        ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
        if (!module.Enable)
        {
            var provider = services.BuildServiceProvider();
            var logger = provider.GetService<ILogger<ModuleApplicationBase>>();
            logger?.LogWarning("{name} is disabled", moduleType.Name);
            return null;
        }
        services.AddSingleton(moduleType, module);
        return module;
    }

    protected void InitializeModules()
    {
        ArgumentNullException.ThrowIfNull(ServiceProvider, nameof(ServiceProvider));
        using var scope = ServiceProvider.CreateScope();
        var ctx = new ApplicationContext(scope.ServiceProvider);
        foreach (var cfg in Modules) cfg.ApplicationInitialization(ctx);
    }
}