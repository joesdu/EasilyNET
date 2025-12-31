using System.Reflection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
// ReSharper disable once UnusedMember.Global
public sealed class DependencyAppModule : AppModule
{
    /// <inheritdoc />
    public override Task ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        var logger = context.ServiceProvider.GetAutoDILogger();
        AddAutoInjection(services, logger);
        return Task.CompletedTask;
    }

    private static void AddAutoInjection(IServiceCollection services, ILogger logger)
    {
        var registry = services.GetOrCreateRegistry();
        var types = AssemblyHelper.FindTypesByAttribute<DependencyInjectionAttribute>().ToList();
        var sortedTypes = TopologicalSort(types);
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Auto-registering {Count} services", sortedTypes.Count);
        }
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
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Registered keyed service: {ServiceType} with key '{Key}'", impl.Name, attr.ServiceKey);
                    }
                    continue;
                }
                // 2. 处理 AsType：注册指定的服务类型为 KeyedService
                if (attr.AsType is not null)
                {
                    if (!impl.IsBaseOn(attr.AsType))
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning("Skipped registration: {Implementation} is not assignable to {ServiceType}", impl.Name, attr.AsType.Name);
                        }
                        continue;
                    }
                    services.AddNamedService(attr.AsType, attr.ServiceKey, impl, lifetime.Value);
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Registered keyed service: {ServiceType} -> {Implementation} with key '{Key}'", attr.AsType.Name, impl.Name, attr.ServiceKey);
                    }
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
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered keyed service: {ServiceType} with key '{Key}'", impl.Name, attr.ServiceKey);
                }
                continue;
            }
            // ====== 普通服务注册逻辑（无 ServiceKey）======
            // 1. 处理 AddSelf + SelfOnly：仅注册自身
            if (attr is { AddSelf: true, SelfOnly: true })
            {
                registry.RegisterImplementation(impl, impl);
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered self-only service: {ServiceType}", impl.Name);
                }
                continue;
            }
            // 2. 处理 AsType：注册指定的服务类型
            if (attr?.AsType is not null)
            {
                if (!impl.IsBaseOn(attr.AsType))
                {
                    if (logger.IsEnabled(LogLevel.Warning))
                    {
                        logger.LogWarning("Skipped registration: {Implementation} is not assignable to {ServiceType}", impl.Name, attr.AsType.Name);
                    }
                    continue;
                }
                registry.RegisterImplementation(attr.AsType, impl);
                services.Add(new(attr.AsType, p => p.CreateInstance(impl), lifetime.Value));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered service: {ServiceType} -> {Implementation}", attr.AsType.Name, impl.Name);
                }
                // AsType 指定后，如果还需要注册自身，需要显式设置 AddSelf
                if (attr.AddSelf)
                {
                    registry.RegisterImplementation(impl, impl);
                    services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                }
                continue;
            }
            // 3. 自动注册接口和抽象类
            var serviceTypes = GetServiceTypes(impl);
            // 3.1 如果需要注册自身（显式指定 AddSelf）
            if (attr?.AddSelf is true)
            {
                registry.RegisterImplementation(impl, impl);
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
            }
            // 3.2 注册所有接口和抽象类
            if (serviceTypes.Count > 0)
            {
                foreach (var serviceType in serviceTypes)
                {
                    registry.RegisterImplementation(serviceType, impl);
                    services.Add(new(serviceType, p => p.CreateInstance(impl), lifetime.Value));
                    if (logger.IsEnabled(LogLevel.Trace))
                    {
                        logger.LogTrace("Registered service: {ServiceType} -> {Implementation}", serviceType.Name, impl.Name);
                    }
                }
            }
            else if (attr?.AddSelf is not true)
            {
                // 3.3 没有接口或抽象类，且未显式声明 AddSelf 时，仅注册自身
                registry.RegisterImplementation(impl, impl);
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered self service: {ServiceType}", impl.Name);
                }
            }
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Completed auto-registration of {Count} services", sortedTypes.Count);
        }
    }

    private static List<Type> TopologicalSort(List<Type> types)
    {
        var typeSet = new HashSet<Type>(types);
        var sorted = new List<Type>(types.Count);
        var visited = new Dictionary<Type, bool>(types.Count);
        foreach (var type in types)
        {
            Visit(type, typeSet, sorted, visited);
        }
        return sorted;
    }

    private static void Visit(Type type, HashSet<Type> types, List<Type> sorted, Dictionary<Type, bool> visited)
    {
        if (visited.TryGetValue(type, out var inProcess))
        {
            if (inProcess)
            {
                throw new InvalidOperationException($"Cyclic dependency found involving type '{type.Name}'.");
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

    private static HashSet<Type> GetServiceTypes(Type implementation)
    {
        var typeInfo = implementation.GetTypeInfo();
        var result = new HashSet<Type>();
        foreach (var iface in typeInfo.ImplementedInterfaces)
        {
            if (iface == typeof(IDisposable) || iface == typeof(IAsyncDisposable))
            {
                continue;
            }
            if (!iface.HasMatchingGenericArity(typeInfo) || iface.HasAttribute<IgnoreDependencyAttribute>())
            {
                continue;
            }
            result.Add(iface.GetRegistrationType(typeInfo));
        }
        return result;
    }
}