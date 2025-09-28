using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en"><see cref="IServiceProvider" /> extensions</para>
///     <para xml:lang="zh"><see cref="IServiceProvider" /> 扩展</para>
/// </summary>
public static class ServiceProviderExtension
{
    // Keyed by (key, service type) to avoid collisions when the same key is used for multiple service types
    internal static readonly ConcurrentDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> NamedServices = [];

    // 非 Keyed 的服务类型到实现类型的映射，用于解析带参数覆盖时确定实现类型
    internal static readonly ConcurrentDictionary<Type, Type> ServiceImplementations = [];

    private static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = [];

    // Dynamic resolve helpers

    /// <param name="provider"></param>
    extension(IServiceProvider provider)
    {
        internal object CreateInstance(Type implementationType)
        {
            var constructor = ConstructorCache.GetOrAdd(implementationType, static t => t.GetConstructors().FirstOrDefault() ?? throw new InvalidOperationException($"No public constructor found for type {t.Name}"));
            var parameters = constructor.GetParameters().Select(p =>
            {
                var keyed = p.CustomAttributes.FirstOrDefault(c => c.AttributeType == typeof(FromKeyedServicesAttribute));
                // ReSharper disable once InvertIf
                if (keyed is not null)
                {
                    var key = keyed.ConstructorArguments.FirstOrDefault().Value;
                    return provider.GetRequiredKeyedService(p.ParameterType, key!);
                }
                return provider.GetService(p.ParameterType) ?? throw new InvalidOperationException($"Unable to resolve service for type '{p.ParameterType}' while attempting to activate '{implementationType}'.");
            }).ToArray();
            return constructor.Invoke(parameters);
        }

        /// <summary>
        /// Create a resolver from the service provider
        /// </summary>
        /// <returns></returns>
        public IResolver CreateResolver() => new Resolver(provider);

        /// <summary>
        /// Resolve a service of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T Resolve<T>() => provider.CreateResolver().Resolve<T>();

        /// <summary>
        /// Resolve a service of the specified type
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public object Resolve(Type serviceType) => provider.CreateResolver().Resolve(serviceType);

        /// <summary>
        /// Resolve an optional service of type T, returning null if not registered
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ResolveOptional<T>() => provider.CreateResolver().ResolveOptional<T>();

        /// <summary>
        /// Try to resolve a service of type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool TryResolve<T>(out T? instance) => provider.CreateResolver().TryResolve(out instance);

        /// <summary>
        /// Try to resolve a service of the specified type
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="instance"></param>
        /// <returns></returns>
        public bool TryResolve(Type serviceType, out object? instance) => provider.CreateResolver().TryResolve(serviceType, out instance);
    }
}