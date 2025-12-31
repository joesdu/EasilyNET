using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">
///     Registry for tracking service implementations and named/keyed services.
///     This class is scoped to an <see cref="IServiceCollection" /> instance to avoid global static state.
///     </para>
///     <para xml:lang="zh">
///     用于跟踪服务实现和命名/键控服务的注册表。
///     此类的作用域限定于 <see cref="IServiceCollection" /> 实例，以避免全局静态状态。
///     </para>
/// </summary>
internal sealed class ServiceRegistry
{
    /// <summary>
    ///     <para xml:lang="en">Named/Keyed services registry, keyed by (key, service type) to avoid collisions</para>
    ///     <para xml:lang="zh">命名/键控服务注册表，以 (key, 服务类型) 为键以避免冲突</para>
    /// </summary>
    private ConcurrentDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> NamedServices { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Service type to implementation type mapping for parameter override resolution</para>
    ///     <para xml:lang="zh">服务类型到实现类型的映射，用于参数覆盖解析</para>
    /// </summary>
    private ConcurrentDictionary<Type, Type> ServiceImplementations { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Register a named/keyed service</para>
    ///     <para xml:lang="zh">注册命名/键控服务</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The service key</para>
    ///     <para xml:lang="zh">服务键</para>
    /// </param>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type</para>
    ///     <para xml:lang="zh">实现类型</para>
    /// </param>
    /// <param name="lifetime">
    ///     <para xml:lang="en">The service lifetime</para>
    ///     <para xml:lang="zh">服务生命周期</para>
    /// </param>
    internal void RegisterNamedService(object key, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        var descriptor = new NamedServiceDescriptor(serviceType, implementationType, lifetime);
        NamedServices[(key, serviceType)] = descriptor;
    }

    /// <summary>
    ///     <para xml:lang="en">Register a service implementation mapping</para>
    ///     <para xml:lang="zh">注册服务实现映射</para>
    /// </summary>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type</para>
    ///     <para xml:lang="zh">实现类型</para>
    /// </param>
    internal void RegisterImplementation(Type serviceType, Type implementationType)
    {
        ServiceImplementations[serviceType] = implementationType;
    }

    /// <summary>
    ///     <para xml:lang="en">Try to get the implementation type for a service type</para>
    ///     <para xml:lang="zh">尝试获取服务类型的实现类型</para>
    /// </summary>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="implementationType">
    ///     <para xml:lang="en">The implementation type if found</para>
    ///     <para xml:lang="zh">如果找到则为实现类型</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if found, false otherwise</para>
    ///     <para xml:lang="zh">如果找到则返回 true，否则返回 false</para>
    /// </returns>
    internal bool TryGetImplementationType(Type serviceType, out Type? implementationType) => ServiceImplementations.TryGetValue(serviceType, out implementationType);

    /// <summary>
    ///     <para xml:lang="en">Try to get a named service descriptor</para>
    ///     <para xml:lang="zh">尝试获取命名服务描述符</para>
    /// </summary>
    /// <param name="key">
    ///     <para xml:lang="en">The service key</para>
    ///     <para xml:lang="zh">服务键</para>
    /// </param>
    /// <param name="serviceType">
    ///     <para xml:lang="en">The service type</para>
    ///     <para xml:lang="zh">服务类型</para>
    /// </param>
    /// <param name="descriptor">
    ///     <para xml:lang="en">The descriptor if found</para>
    ///     <para xml:lang="zh">如果找到则为描述符</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if found, false otherwise</para>
    ///     <para xml:lang="zh">如果找到则返回 true，否则返回 false</para>
    /// </returns>
    internal bool TryGetNamedService(object key, Type serviceType, out NamedServiceDescriptor? descriptor) => NamedServices.TryGetValue((key, serviceType), out descriptor);
}