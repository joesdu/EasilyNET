using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
public class AppModule : IAppModule
{
    /// <inheritdoc />
    public virtual Task ConfigureServices(ConfigureServicesContext context) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task ApplicationInitialization(ApplicationContext context) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual bool GetEnable(ConfigureServicesContext context) => true;

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