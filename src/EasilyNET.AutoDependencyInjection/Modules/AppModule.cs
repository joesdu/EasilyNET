using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <summary>
/// AppModule
/// </summary>
public class AppModule : IAppModule
{
    /// <summary>
    /// 是否启用,默认为true
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
        var dependedTypes = moduleType.GetCustomAttributes().OfType<IDependedTypesProvider>().ToArray();
        if (dependedTypes.Length == 0) return Array.Empty<Type>();
        List<Type> dependList = new();
        foreach (var dependedType in dependedTypes)
        {
            var depends = dependedType.GetDependedTypes().ToArray();
            if (depends.Length == 0) continue;
            dependList.AddRange(depends);
            foreach (var type in depends) dependList.AddRange(GetDependedTypes(type));
        }

        return dependList.Distinct();
    }

    /// <summary>
    /// 判断是否是模块
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsAppModule(Type type)
    {
        var typeInfo = type.GetTypeInfo();
        return typeInfo is {IsClass: true, IsAbstract: false, IsGenericType: false} && typeof(IAppModule).GetTypeInfo().IsAssignableFrom(type);
    }
}