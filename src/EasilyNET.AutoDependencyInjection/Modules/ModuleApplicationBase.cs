using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <summary>
/// 模块应用基础
/// </summary>
internal class ModuleApplicationBase : IModuleApplication
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="startupModuleType"></param>
    /// <param name="services"></param>
    protected ModuleApplicationBase(Type startupModuleType, IServiceCollection services)
    {
        ServiceProvider = null;
        StartupModuleType = startupModuleType;
        Services = services;
        _ = services.AddSingleton<IModuleApplication>(this);
        _ = services.TryAddObjectAccessor<IServiceProvider>();
        Source = GetEnabledAllModule(services);
        Modules = LoadModules;
    }

    /// <summary>
    /// 获取所有需要加载的模块
    /// </summary>
    /// <returns></returns>
    private IReadOnlyList<IAppModule> LoadModules
    {
        get
        {
            List<IAppModule> modules = new();
            var module = Source.FirstOrDefault(o => o.GetType() == StartupModuleType) ?? throw new($"类型为“{StartupModuleType.FullName}”的模块实例无法找到");
            modules.Add(module);
            var depends = module.GetDependedTypes();
            foreach (var dependType in depends.Where(AppModule.IsAppModule))
            {
                var dependModule = Source.ToList().Find(m => m.GetType() == dependType);
                if (dependModule is null)
                    continue;
                if (!modules.Contains(dependModule))
                    modules.Add(dependModule);
            }
            return modules;
        }
    }

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
    public IServiceProvider? ServiceProvider { get; private set; }

    /// <summary>
    /// 模块接口容器
    /// </summary>
    public IReadOnlyList<IAppModule> Modules { get; set; }

    /// <summary>
    /// Source
    /// </summary>
    public IEnumerable<IAppModule> Source { get; }

    /// <summary>
    /// Dispose
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 获取所有启用的模块
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    private static IEnumerable<IAppModule> GetEnabledAllModule(IServiceCollection services)
    {
        var types = AssemblyHelper.FindTypes(AppModule.IsAppModule);
        var modules = types.Select(o => CreateModule(services, o)).Where(c => c is not null);
        return modules.Distinct()!;
    }

    /// <summary>
    /// 设置ServiceProvider
    /// </summary>
    /// <param name="serviceProvider"></param>
    protected void SetServiceProvider(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        ServiceProvider.GetRequiredService<ObjectAccessor<IServiceProvider>>().Value = ServiceProvider;
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
#if NETSTANDARD
        var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule ?? throw new ArgumentNullException(nameof(moduleType));
#else
        var module = Expression.Lambda(Expression.New(moduleType)).Compile().DynamicInvoke() as IAppModule;
        ArgumentNullException.ThrowIfNull(module, nameof(moduleType));
#endif
        if (!module.Enable) return null;
        _ = services.AddSingleton(moduleType, module);
        return module;
    }
}