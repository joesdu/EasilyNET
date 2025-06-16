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
            if (attr?.AsType is not null)
            {
                // 若实现类,不是注册类型的派生类,则跳过
                if (impl.IsBaseOn(attr.AsType))
                {
                    if (attr.ServiceKey is not null)
                    {
                        services.AddNamedService(attr.AsType, attr.ServiceKey, impl, lifetime.Value);
                    }
                    else
                    {
                        services.Add(new(attr.AsType, p => p.CreateInstance(impl), lifetime.Value));
                    }
                }
                continue;
            }
            var serviceTypes = GetServiceTypes(impl);
            if (serviceTypes.Count is 0 || attr?.AddSelf is true)
            {
                if (attr?.ServiceKey is not null)
                {
                    services.AddNamedService(impl, attr.ServiceKey, impl, lifetime.Value);
                }
                else
                {
                    services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                }
                if (attr?.SelfOnly is true || serviceTypes.Count is 0)
                {
                    continue;
                }
            }
            foreach (var serviceType in serviceTypes)
            {
                if (attr?.ServiceKey is not null)
                {
                    services.AddNamedService(serviceType, attr.ServiceKey, impl, lifetime.Value);
                }
                else
                {
                    services.Add(new(serviceType, p => p.CreateInstance(impl), lifetime.Value));
                }
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