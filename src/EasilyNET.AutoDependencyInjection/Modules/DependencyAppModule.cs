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
        var sortedTypes = TopologicalSort(types);
        foreach (var impl in sortedTypes)
        {
            var attr = impl.GetCustomAttribute<DependencyInjectionAttribute>();
            var lifetime = attr?.Lifetime;
            if (lifetime is null)
            {
                continue;
            }
            // ====== KeyedService 注册逻辑 ======
            if (attr?.ServiceKey is not null)
            {
                // 1. 处理 SelfOnly：仅注册自身为 KeyedService
                if (attr.SelfOnly)
                {
                    services.AddNamedService(impl, attr.ServiceKey, impl, lifetime.Value);
                    continue;
                }
                // 2. 处理 AsType：注册指定的服务类型为 KeyedService
                if (attr.AsType is not null)
                {
                    if (!impl.IsBaseOn(attr.AsType))
                    {
                        continue;
                    }
                    services.AddNamedService(attr.AsType, attr.ServiceKey, impl, lifetime.Value);
                    // AsType 指定后，如果还需要注册自身，需要显式设置 AddSelf
                    if (attr.AddSelf)
                    {
                        services.AddNamedService(impl, attr.ServiceKey, impl, lifetime.Value);
                    }
                    continue;
                }
                // 3. 当使用 ServiceKey 但没有指定 SelfOnly 或 AsType 时
                // 为了避免语义混乱，必须明确指定注册策略
                // 默认行为：仅注册自身（与 SelfOnly 相同）
                services.AddNamedService(impl, attr.ServiceKey, impl, lifetime.Value);
                continue;
            }
            // ====== 普通服务注册逻辑（无 ServiceKey）======
            // 1. 处理 AddSelf + SelfOnly：仅注册自身
            if (attr is { AddSelf: true, SelfOnly: true })
            {
                ServiceProviderExtension.ServiceImplementations[impl] = impl;
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                continue;
            }
            // 2. 处理 AsType：注册指定的服务类型
            if (attr?.AsType is not null)
            {
                if (!impl.IsBaseOn(attr.AsType))
                {
                    continue;
                }
                ServiceProviderExtension.ServiceImplementations[attr.AsType] = impl;
                services.Add(new(attr.AsType, p => p.CreateInstance(impl), lifetime.Value));
                // AsType 指定后，如果还需要注册自身，需要显式设置 AddSelf
                if (attr.AddSelf)
                {
                    ServiceProviderExtension.ServiceImplementations[impl] = impl;
                    services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                }
                continue;
            }
            // 3. 自动注册接口和抽象类
            var serviceTypes = GetServiceTypes(impl);
            // 3.1 如果需要注册自身（显式指定 AddSelf）
            if (attr?.AddSelf is true)
            {
                ServiceProviderExtension.ServiceImplementations[impl] = impl;
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
            }
            // 3.2 注册所有接口和抽象类
            if (serviceTypes.Count > 0)
            {
                foreach (var serviceType in serviceTypes)
                {
                    ServiceProviderExtension.ServiceImplementations[serviceType] = impl;
                    services.Add(new(serviceType, p => p.CreateInstance(impl), lifetime.Value));
                }
            }
            else if (attr?.AddSelf is not true)
            {
                // 3.3 没有接口或抽象类，且未显式声明 AddSelf 时，仅注册自身
                ServiceProviderExtension.ServiceImplementations[impl] = impl;
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
            }
        }
    }

    private static List<Type> TopologicalSort(HashSet<Type> types)
    {
        var sorted = new List<Type>();
        var visited = new Dictionary<Type, bool>();
        foreach (var type in types)
        {
            Visit(type, types, sorted, visited);
        }
        return sorted;
    }

    private static void Visit(Type type, HashSet<Type> types, List<Type> sorted, Dictionary<Type, bool> visited)
    {
        if (visited.TryGetValue(type, out var inProcess))
        {
            if (inProcess)
            {
                throw new InvalidOperationException("Cyclic dependency found");
            }
            return;
        }
        visited[type] = true;
        var dependencies = GetServiceTypes(type);
        foreach (var dependency in dependencies.Where(types.Contains))
        {
            Visit(dependency, types, sorted, visited);
        }
        visited[type] = false;
        sorted.Add(type);
    }

    private static FrozenSet<Type> GetServiceTypes(Type implementation)
    {
        var typeInfo = implementation.GetTypeInfo();
        return typeInfo.ImplementedInterfaces
                       .Where(x => x.HasMatchingGenericArity(typeInfo) && !x.HasAttribute<IgnoreDependencyAttribute>() && x != typeof(IDisposable))
                       .Select(t => t.GetRegistrationType(typeInfo)).ToFrozenSet();
    }
}