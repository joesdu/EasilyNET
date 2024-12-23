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

    private static void AddAutoInjection(IServiceCollection services)
    {
        var types = AssemblyHelper.FindTypesByAttribute<DependencyInjectionAttribute>().ToHashSet();
        foreach (var implementedType in types)
        {
            var attr = implementedType.GetCustomAttribute<DependencyInjectionAttribute>();
            var lifetime = attr?.Lifetime;
            if (lifetime is null) continue;
            if (attr?.AsType is not null)
            {
                // 若实现类,不是注册类型的派生类,则跳过
                if (implementedType.IsBaseOn(attr.AsType))
                {
                    if (!string.IsNullOrWhiteSpace(attr.ServiceKey))
                    {
                        services.Add(new(implementedType, attr.ServiceKey, implementedType, lifetime.Value));
                    }
                    else
                    {
                        services.Add(new(attr.AsType, implementedType, lifetime.Value));
                    }
                }
                continue;
            }
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
                if (attr?.SelfOnly is true || serviceTypes.Count is 0) continue;
            }
            foreach (var serviceType in serviceTypes)
            {
                if (!string.IsNullOrWhiteSpace(attr?.ServiceKey))
                {
                    services.Add(new(serviceType, attr.ServiceKey, implementedType, lifetime.Value));
                }
                else
                {
                    services.Add(new(serviceType, implementedType, lifetime.Value));
                }
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
}