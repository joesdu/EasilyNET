using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
public class AppModule : IAppModule
{
    /// <summary>
    /// 是否启用,默认为 <see langword="true" />
    /// </summary>
    public bool Enable { get; set; } = true;

    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
    public virtual void ConfigureServices(ConfigureServicesContext context) { }

    /// <summary>
    /// 应用程序初始化
    /// </summary>
    /// <param name="context"></param>
    public virtual void ApplicationInitialization(ApplicationContext context) { }

    /// <summary>
    /// 获取模块程序集
    /// </summary>
    /// <param name="moduleType"></param>
    /// <returns></returns>
    public IEnumerable<Type> GetDependedTypes(Type? moduleType = null)
    {
        moduleType ??= GetType();
        var dependedTypes = moduleType.GetCustomAttributes().OfType<IDependedTypesProvider>();
        if (!dependedTypes.Any()) return [];
        var dependSet = new HashSet<Type>();
        var stack = new Stack<Type>([moduleType]);
        while (stack.Count > 0)
        {
            var currentType = stack.Pop();
            if (!dependSet.Add(currentType)) continue;
            var cdt = currentType.GetCustomAttributes().OfType<IDependedTypesProvider>();
            foreach (var dependedType in cdt)
            {
                var depends = dependedType.GetDependedTypes();
                foreach (var type in depends)
                {
                    if (dependSet.Add(type))
                    {
                        stack.Push(type);
                    }
                }
            }
        }
        return dependSet;
    }

    /// <summary>
    /// 判断是否是模块
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsAppModule(Type type)
    {
        var typeInfo = type.GetTypeInfo();
        return typeInfo is { IsClass: true, IsAbstract: false, IsGenericType: false } && typeof(IAppModule).GetTypeInfo().IsAssignableFrom(type);
    }
}