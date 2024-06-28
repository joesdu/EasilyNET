using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Frozen;
using System.Reflection;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <summary>
/// 自动注入模块，继承与AppModuleBase类进行实现
/// </summary>
// ReSharper disable once UnusedMember.Global
public sealed class DependencyAppModule : AppModule
{
    /// <summary>
    /// 注册服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        AddAutoInjection(services);
    }

    /// <summary>
    /// 添加自动注入
    /// </summary>
    /// <param name="services"></param>
    private static void AddAutoInjection(IServiceCollection services)
    {
        var baseTypes = new[] { typeof(IScopedDependency), typeof(ITransientDependency), typeof(ISingletonDependency) };
        var types = AssemblyHelper.FindTypes(type =>
            (type is { IsClass: true, IsAbstract: false } && baseTypes.Any(b => b.IsAssignableFrom(type))) ||
            type.GetCustomAttribute<DependencyInjectionAttribute>() is not null);
        foreach (var implementedType in types)
        {
            var attr = implementedType.GetCustomAttribute<DependencyInjectionAttribute>();
            var lifetime = GetServiceLifetime(implementedType);
            if (lifetime is null) continue;
            // 优化：直接从属性或特性获取AddSelf和SelfOnly的值,这里的名称属于约定项
            var addSelf = attr?.AddSelf ?? GetPropertyValue<bool?>(implementedType, "DependencyInjectionSelf");
            var serviceTypes = GetServiceTypes(implementedType);
            if (serviceTypes.Count is 0 || addSelf is true)
            {
                services.Add(new(implementedType, implementedType, lifetime.Value));
                var selfOnly = attr?.SelfOnly ?? GetPropertyValue<bool?>(implementedType, "DependencyInjectionSelfOnly");
                if (selfOnly is true || serviceTypes.Count is 0) continue;
            }
            foreach (var serviceType in serviceTypes.Where(o => !o.HasAttribute<IgnoreDependencyAttribute>()))
            {
                services.Add(new(serviceType, implementedType, lifetime.Value));
            }
        }
    }

    private static FrozenSet<Type> GetServiceTypes(Type implementation)
    {
        var typeInfo = implementation.GetTypeInfo();
        return typeInfo.ImplementedInterfaces
                       .Where(x => x.HasMatchingGenericArity(typeInfo) && !x.HasAttribute<IgnoreDependencyAttribute>() && x != typeof(IDisposable))
                       .Select(t => t.GetRegistrationType(typeInfo)).ToFrozenSet();
    }

    // 优化：提取获取静态属性值的通用方法，减少重复代码
    private static T? GetPropertyValue<T>(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return property is null ? default : (T?)property.GetValue(type);
    }

    /// <summary>
    /// 获取服务生命周期
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static ServiceLifetime? GetServiceLifetime(Type type)
    {
        var attr = type.GetCustomAttribute<DependencyInjectionAttribute>();
        return attr?.Lifetime ??
               (typeof(IScopedDependency).IsAssignableFrom(type)
                    ? ServiceLifetime.Scoped
                    : typeof(ITransientDependency).IsAssignableFrom(type)
                        ? ServiceLifetime.Transient
                        : typeof(ISingletonDependency).IsAssignableFrom(type)
                            ? ServiceLifetime.Singleton
                            : null);
    }

    /// <summary>
    /// 应用初始化,通常用来注册中间件.
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        context.GetApplicationHost();
    }
}