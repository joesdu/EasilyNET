using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">Descriptor for a named/keyed service registration</para>
///     <para xml:lang="zh">命名/键控服务注册的描述符</para>
/// </summary>
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
internal sealed class NamedServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
{
    /// <summary>
    ///     <para xml:lang="en">Gets the service type</para>
    ///     <para xml:lang="zh">获取服务类型</para>
    /// </summary>
    public Type ServiceType { get; } = serviceType;

    /// <summary>
    ///     <para xml:lang="en">Gets the implementation type</para>
    ///     <para xml:lang="zh">获取实现类型</para>
    /// </summary>
    public Type ImplementationType { get; } = implementationType;

    /// <summary>
    ///     <para xml:lang="en">Gets the service lifetime</para>
    ///     <para xml:lang="zh">获取服务生命周期</para>
    /// </summary>
    public ServiceLifetime Lifetime { get; } = lifetime;
}