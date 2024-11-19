using System.Collections.Frozen;
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;

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
        var types = AssemblyHelper.FindTypesByAttribute<DependencyInjectionAttribute>();
        Parallel.ForEach(types, implementedType =>
        {
            var attr = implementedType.GetCustomAttribute<DependencyInjectionAttribute>();
            var lifetime = GetServiceLifetime(implementedType);
            if (lifetime is null) return;
            var serviceTypes = GetServiceTypes(implementedType);
            if (serviceTypes.Count is 0 || attr?.AddSelf is true)
            {
                if (!string.IsNullOrWhiteSpace(attr?.ServiceKey))
                {
                    services.Add(new(implementedType, attr.ServiceKey, implementedType, lifetime.Value));
                }
                else
                {
                    services.Add(new(implementedType, implementedType, lifetime.Value));
                }
                if (attr?.SelfOnly is true || serviceTypes.Count is 0) return;
            }
            Parallel.ForEach(serviceTypes, serviceType =>
            {
                if (!string.IsNullOrWhiteSpace(attr?.ServiceKey))
                {
                    services.Add(new(serviceType, attr.ServiceKey, implementedType, lifetime.Value));
                }
                services.Add(new(serviceType, implementedType, lifetime.Value));
            });
        });
    }

    private static FrozenSet<Type> GetServiceTypes(Type implementation)
    {
        var typeInfo = implementation.GetTypeInfo();
        return typeInfo.ImplementedInterfaces
                       .Where(x => x.HasMatchingGenericArity(typeInfo) && !x.HasAttribute<IgnoreDependencyAttribute>() && x != typeof(IDisposable))
                       .Select(t => t.GetRegistrationType(typeInfo)).ToFrozenSet();
    }

    /// <summary>
    /// 获取服务生命周期
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private static ServiceLifetime? GetServiceLifetime(Type type)
    {
        var attr = type.GetCustomAttribute<DependencyInjectionAttribute>();
        return attr?.Lifetime;
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