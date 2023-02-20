using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.AutoDependencyInjection.Extensions;
/// <summary>
/// IServiceCollection扩展
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// 得到注入服务
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    public static T? GetService<T>(this IServiceCollection services) => services.BuildServiceProvider().GetService<T>();
    /// <summary>
    /// 获取IServiceCollection服务
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IConfiguration GetConfiguration(this IServiceCollection services) => services.GetBuildService<IConfiguration>() ?? throw new("未找到IConfiguration服务");
    /// <summary>
    /// 得到注入服务
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
            : descriptor?.ImplementationFactory is not null ? (T)descriptor.ImplementationFactory.Invoke(null!) : default;
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
    /// 试着添加对象适配器
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="services"></param>
    /// <returns></returns>
    internal static ObjectAccessor<T> TryAddObjectAccessor<T>(this IServiceCollection services) => services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>))
            ? services.GetSingletonInstance<ObjectAccessor<T>>()
            : services.AddObjectAccessor<T>();
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
        if (services.Any(s => s.ServiceType == typeof(ObjectAccessor<T>)))
        {
            throw new("在类型“{typeof(T).AssemblyQualifiedName)}”之前注册了对象");
        }
        //Add to the beginning for fast retrieve
        services.Insert(0, ServiceDescriptor.Singleton(typeof(ObjectAccessor<T>), accessor));
        services.Insert(0, ServiceDescriptor.Singleton(typeof(IObjectAccessor<T>), accessor));
        return accessor;
    }
    /// <summary>
    /// 从工厂创建服务适配器
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceProvider BuildServiceProviderFromFactory(this IServiceCollection services)
    {
        foreach (var service in services)
        {
            var factoryInterface = service.ImplementationInstance?.GetType()
                .GetTypeInfo()
                .GetInterfaces()
                .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IServiceProviderFactory<>));
            if (factoryInterface is null) continue;
            var containerBuilderType = factoryInterface.GenericTypeArguments[0];
            return (IServiceProvider)typeof(ServiceCollectionExtension)
                .GetTypeInfo()
                .GetMethods()
                .Single(m => m is { Name: nameof(BuildServiceProviderFromFactory), IsGenericMethod: true })
                .MakeGenericMethod(containerBuilderType)
                .Invoke(null, new object[] { services, null! })!;
        }
        return services.BuildServiceProvider();
    }
}