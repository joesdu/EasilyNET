using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Abstractions;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
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
        foreach (var implementedInterType in types)
        {
            var attr = implementedInterType.GetCustomAttribute<DependencyInjectionAttribute>();
            var typeInfo = implementedInterType.GetTypeInfo();
            var serviceTypes = typeInfo.ImplementedInterfaces
                                       .Where(x => x.HasMatchingGenericArity(typeInfo) && !x.HasAttribute<IgnoreDependencyAttribute>() && x != typeof(IDisposable))
                                       .Select(t => t.GetRegistrationType(typeInfo)).ToList();
            var lifetime = GetServiceLifetime(implementedInterType);
            if (lifetime is null) continue;
            if (serviceTypes.Count is 0)
            {
                services.Add(new(implementedInterType, implementedInterType, lifetime.Value));
                continue;
            }
            bool? addSelf = attr?.AddSelf ?? false;
            if (addSelf is not true)
            {
                var addSelfProperty = implementedInterType.GetProperty("AddSelf", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                addSelf = (bool?)(addSelfProperty?.GetValue(implementedInterType) ?? false);
            }
            if (addSelf is true)
            {
                services.Add(new(implementedInterType, implementedInterType, lifetime.Value));
            }
            //var addSelf = (bool?)(implementedInterType.GetProperty("AddSelf", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)?.GetValue(implementedInterType) ?? false);
            //if (attr?.AddSelf is true || addSelf is true) services.Add(new(implementedInterType, implementedInterType, lifetime.Value));
            foreach (var serviceType in serviceTypes.Where(o => !o.HasAttribute<IgnoreDependencyAttribute>()))
            {
                services.Add(new(serviceType, implementedInterType, lifetime.Value));
            }
        }
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