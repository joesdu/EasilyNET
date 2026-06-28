using System.Reflection;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.Misc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Modules;

/// <inheritdoc />
// ReSharper disable once UnusedMember.Global
public sealed class DependencyAppModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var services = context.Services;
        // 使用空记录器以避免在注册阶段构建临时 ServiceProvider 带来的内存泄漏与重复单例问题
        AddAutoInjection(services, NullLogger.Instance);
    }

    private static void AddAutoInjection(IServiceCollection services, ILogger logger)
    {
        var registry = services.GetOrCreateRegistry();
        // Registration order is irrelevant to Microsoft.Extensions.DependencyInjection resolution,
        // so we register in discovery order. (A prior interface-graph "topological sort" added no value
        // and could throw a false "cyclic dependency" error.)
        var types = AssemblyHelper.FindTypesByAttribute<DependencyInjectionAttribute>().ToList();
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Auto-registering {Count} services", types.Count);
        }
        foreach (var impl in types)
        {
            var attr = impl.GetCustomAttribute<DependencyInjectionAttribute>();
            var lifetime = attr?.Lifetime;
            if (lifetime is null)
            {
                continue;
            }
            // A type that cannot be instantiated (abstract / interface / open generic) would throw at
            // resolve time when used with a CreateInstance factory, so skip it with a warning.
            if (impl.IsAbstract || impl.IsInterface || impl.ContainsGenericParameters)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning("Skipped registration: {Implementation} is abstract, an interface, or an open generic and cannot be instantiated.", impl.Name);
                }
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
            // 1. SelfOnly：仅注册自身（忽略接口/抽象基类），无论是否设置 AddSelf。
            if (attr is { SelfOnly: true })
            {
                registry.RegisterImplementation(impl, impl);
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered self-only service: {ServiceType}", impl.Name);
                }
                continue;
            }
            // 2. 计算需要暴露的服务类型集合：AsType 显式指定，或自动发现接口/抽象基类。
            List<Type> serviceTypes;
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
                serviceTypes = [attr.AsType];
            }
            else
            {
                serviceTypes = [.. GetServiceTypes(impl)];
            }
            // 3. 当需要 AddSelf、没有可暴露的服务类型（回退为仅自身），或存在多个服务类型时，
            //    先以具体类型注册一个“锚点”，其余服务类型通过 GetRequiredService 转发到该锚点，
            //    从而保证 Singleton/Scoped 下所有服务类型共享同一实例（修复多接口产生多个单例实例的缺陷）。
            var addSelf = attr?.AddSelf is true;
            var needAnchor = addSelf || serviceTypes.Count != 1;
            if (needAnchor)
            {
                registry.RegisterImplementation(impl, impl);
                services.Add(new(impl, p => p.CreateInstance(impl), lifetime.Value));
            }
            foreach (var serviceType in serviceTypes)
            {
                registry.RegisterImplementation(serviceType, impl);
                services.Add(needAnchor
                    ? new ServiceDescriptor(serviceType, p => p.GetRequiredService(impl), lifetime.Value)
                    : new ServiceDescriptor(serviceType, p => p.CreateInstance(impl), lifetime.Value));
                if (logger.IsEnabled(LogLevel.Trace))
                {
                    logger.LogTrace("Registered service: {ServiceType} -> {Implementation}", serviceType.Name, impl.Name);
                }
            }
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Completed auto-registration of {Count} services", types.Count);
        }
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