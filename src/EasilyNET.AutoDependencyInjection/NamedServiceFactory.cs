using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">Factory for creating named/keyed service instances</para>
///     <para xml:lang="zh">用于创建命名/键控服务实例的工厂</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The service type</para>
///     <para xml:lang="zh">服务类型</para>
/// </typeparam>
internal sealed class NamedServiceFactory<T>(IServiceProvider provider) : INamedServiceFactory<T>
{
    /// <inheritdoc />
    public T Create(object key, params Parameter[] parameters)
    {
        // 对于无参数的情况，直接使用内置的 keyed service 解析，避免创建 Resolver
        if (parameters.Length == 0)
        {
            return (T)provider.GetRequiredKeyedService(typeof(T), key);
        }
        // 需要参数覆盖时才创建 Resolver
        using var resolver = new Resolver(provider, provider.GetRequiredService<ServiceRegistry>());
        return resolver.ResolveKeyed<T>(key, parameters);
    }
}