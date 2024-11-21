using System.Collections.Frozen;
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
// ReSharper disable once UnusedMember.Global
public sealed class DependencyAppModule : AppModule
{
    /// <inheritdoc />
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        AddAutoInjection(services);
        await Task.CompletedTask;
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
}