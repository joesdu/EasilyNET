using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedMember.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// <see cref="IServiceCollection" /> 扩展
/// </summary>
public static partial class ServiceCollectionExtension
{
    /// <summary>
    /// 添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    private static void AddObjectAccessor<T>(this IServiceCollection services) => services.AddObjectAccessor(new ObjectAccessor<T>());

    /// <summary>
    /// 添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="accessor"></param>
    /// <returns></returns>
    private static void AddObjectAccessor<T>(this IServiceCollection services, IObjectAccessor<T> accessor)
    {
        if (services.Any(s => s.ServiceType == typeof(IObjectAccessor<T>))) throw new($"{typeof(T).AssemblyQualifiedName}已注册");
        //Add to the beginning for fast retrieve
        services.AddSingleton(accessor);
    }
}