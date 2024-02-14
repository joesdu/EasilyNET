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
    /// 得到已注入的服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    private static T? GetBuildService<T>(this IServiceCollection services)
    {
        var provider = services.BuildServiceProvider();
        return provider.GetService<T>();
    }

    /// <summary>
    /// 获取单例注册服务对象
    /// </summary>
    private static T? GetSingletonInstanceOrNull<T>(this IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T) && d.Lifetime == ServiceLifetime.Singleton);
        return descriptor?.ImplementationInstance is not null
                   ? (T)descriptor.ImplementationInstance
                   : descriptor?.ImplementationFactory is not null
                       ? (T)descriptor.ImplementationFactory.Invoke(null!)
                       : default;
    }

    /// <summary>
    /// 获取单列实例
    /// </summary>
    /// <typeparam name="T">服务类型</typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private static T GetSingletonInstance<T>(this IServiceCollection services)
    {
        var service = services.GetSingletonInstanceOrNull<T>();
        return service is null ? throw new InvalidOperationException($"找不到Singleton服务: {typeof(T).AssemblyQualifiedName}") : service;
    }

    /// <summary>
    /// 添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    private static ObjectAccessor<T> AddObjectAccessor<T>(this IServiceCollection services) => services.AddObjectAccessor(new ObjectAccessor<T>());

    /// <summary>
    /// 添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <param name="accessor"></param>
    /// <returns></returns>
    private static ObjectAccessor<T> AddObjectAccessor<T>(this IServiceCollection services, ObjectAccessor<T> accessor)
    {
        if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>))) throw new("在类型“{typeof(T).AssemblyQualifiedName)}”之前注册了对象");
        //Add to the beginning for fast retrieve
        services.Insert(0, ServiceDescriptor.Singleton(typeof(ObjectAccessor<T>), accessor));
        services.Insert(0, ServiceDescriptor.Singleton(typeof(IObjectAccessor<T>), accessor));
        return accessor;
    }
}
