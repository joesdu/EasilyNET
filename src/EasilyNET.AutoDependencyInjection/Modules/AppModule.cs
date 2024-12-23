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
    ///     <para xml:lang="en">Get the dependent types of the module</para>
    ///     <para xml:lang="zh">获取模块的依赖类型</para>
    /// </summary>
    /// <param name="moduleType">
    ///     <para xml:lang="en">Type of the module</para>
    ///     <para xml:lang="zh">模块的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">Collection of dependent types</para>
    ///     <para xml:lang="zh">依赖类型集合</para>
    /// </returns>
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
    ///     <para xml:lang="en">Determine if a type is a module</para>
    ///     <para xml:lang="zh">判断类型是否是模块</para>
    /// </summary>
    /// <param name="type">
    ///     <para xml:lang="en">Type to check</para>
    ///     <para xml:lang="zh">要检查的类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the type is a module, otherwise false</para>
    ///     <para xml:lang="zh">如果类型是模块则返回 true，否则返回 false</para>
    /// </returns>
    public static bool IsAppModule(Type type)
    {
        var typeInfo = type.GetTypeInfo();
        return typeInfo is { IsClass: true, IsAbstract: false, IsGenericType: false } && typeof(IAppModule).GetTypeInfo().IsAssignableFrom(type);
    }
}