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
    internal static readonly ConcurrentDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> NamedServices = new();

    // 非 Keyed 的服务类型到实现类型的映射，用于解析带参数覆盖时确定实现类型
    internal static readonly ConcurrentDictionary<Type, Type> ServiceImplementations = new();

    private static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = new();

    /// <param name="provider">Service provider.</param>
    extension(IServiceProvider provider)
    {
        /// <summary>
        ///     <para xml:lang="en">Create a resolver wrapper for the current provider.</para>
        ///     <para xml:lang="zh">基于当前 <see cref="IServiceProvider" /> 创建解析器。</para>
        /// </summary>
        /// <param name="createScope">Whether to create an isolated scope for the resolver.</param>
        public IResolver CreateResolver(bool createScope = false)
        {
            ArgumentNullException.ThrowIfNull(provider);
            if (!createScope)
            {
                return new Resolver(provider);
            }
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            return new Resolver(scope.ServiceProvider, scope);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve type dynamically.</para>
        ///     <para xml:lang="zh">动态解析指定类型的服务。</para>
        /// </summary>
        /// <param name="serviceType">
        ///     <para xml:lang="en">The type of service to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance.</para>
        ///     <para xml:lang="zh">解析的服务实例。</para>
        /// </returns>
        public object Resolve(Type serviceType)
        {
            using var resolver = provider.CreateResolver();
            return resolver.Resolve(serviceType);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve with constructor parameter overrides.</para>
        ///     <para xml:lang="zh">使用构造函数参数覆盖来解析服务。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <param name="parameters">
        ///     <para xml:lang="en">Constructor parameters to override.</para>
        ///     <para xml:lang="zh">要覆盖的构造函数参数。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance.</para>
        ///     <para xml:lang="zh">解析的服务实例。</para>
        /// </returns>
        public T Resolve<T>(params Parameter[] parameters)
        {
            if (parameters.Length > 0)
            {
                using var resolver = provider.CreateResolver();
                return resolver.Resolve<T>(parameters);
            }
            else
            {
                using var resolver = provider.CreateResolver();
                return resolver.Resolve<T>();
            }
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve with constructor parameter overrides.</para>
        ///     <para xml:lang="zh">使用构造函数参数覆盖来解析服务。</para>
        /// </summary>
        /// <param name="serviceType">
        ///     <para xml:lang="en">The type of service to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">Constructor parameters to override.</para>
        ///     <para xml:lang="zh">要覆盖的构造函数参数。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance.</para>
        ///     <para xml:lang="zh">解析的服务实例。</para>
        /// </returns>
        public object Resolve(Type serviceType, params Parameter[] parameters)
        {
            ArgumentNullException.ThrowIfNull(parameters);
            using var resolver = provider.CreateResolver();
            return resolver.Resolve(serviceType, parameters);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve all registrations for <typeparamref name="T" />.</para>
        ///     <para xml:lang="zh">解析 <typeparamref name="T" /> 的所有注册。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <returns>
        ///     <para xml:lang="en">A collection of all registered service instances.</para>
        ///     <para xml:lang="zh">所有已注册服务实例的集合。</para>
        /// </returns>
        public IEnumerable<T> ResolveAll<T>()
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveAll<T>();
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve optional service.</para>
        ///     <para xml:lang="zh">解析可选服务。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance, or null if the service is not registered.</para>
        ///     <para xml:lang="zh">解析的服务实例，如果服务未注册则返回 null。</para>
        /// </returns>
        public T? ResolveOptional<T>()
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveOptional<T>();
        }

        /// <summary>
        ///     <para xml:lang="en">Try resolve service.</para>
        ///     <para xml:lang="zh">尝试解析服务。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <param name="instance">
        ///     <para xml:lang="en">The resolved service instance, or null if resolution fails.</para>
        ///     <para xml:lang="zh">解析的服务实例，如果解析失败则为 null。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True if the service was successfully resolved; otherwise, false.</para>
        ///     <para xml:lang="zh">如果服务成功解析则返回 true，否则返回 false。</para>
        /// </returns>
        public bool TryResolve<T>(out T? instance)
        {
            using var resolver = provider.CreateResolver();
            return resolver.TryResolve(out instance);
        }

        /// <summary>
        ///     <para xml:lang="en">Try resolve service by type.</para>
        ///     <para xml:lang="zh">按类型尝试解析服务。</para>
        /// </summary>
        /// <param name="serviceType">
        ///     <para xml:lang="en">The type of service to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </param>
        /// <param name="instance">
        ///     <para xml:lang="en">The resolved service instance, or null if resolution fails.</para>
        ///     <para xml:lang="zh">解析的服务实例，如果解析失败则为 null。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">True if the service was successfully resolved; otherwise, false.</para>
        ///     <para xml:lang="zh">如果服务成功解析则返回 true，否则返回 false。</para>
        /// </returns>
        public bool TryResolve(Type serviceType, out object? instance)
        {
            using var resolver = provider.CreateResolver();
            return resolver.TryResolve(serviceType, out instance);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve named registration.</para>
        ///     <para xml:lang="zh">解析命名注册的服务。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <param name="name">
        ///     <para xml:lang="en">The name of the registered service.</para>
        ///     <para xml:lang="zh">已注册服务的名称。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">Optional constructor parameters to override.</para>
        ///     <para xml:lang="zh">可选的构造函数参数覆盖。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance.</para>
        ///     <para xml:lang="zh">解析的服务实例。</para>
        /// </returns>
        public T ResolveNamed<T>(string name, params Parameter[]? parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveNamed<T>(name, parameters);
        }

        /// <summary>
        ///     <para xml:lang="en">Resolve keyed registration.</para>
        ///     <para xml:lang="zh">解析键控注册的服务。</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <param name="key">
        ///     <para xml:lang="en">The key of the registered service.</para>
        ///     <para xml:lang="zh">已注册服务的键。</para>
        /// </param>
        /// <param name="parameters">
        ///     <para xml:lang="en">Optional constructor parameters to override.</para>
        ///     <para xml:lang="zh">可选的构造函数参数覆盖。</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">The resolved service instance.</para>
        ///     <para xml:lang="zh">解析的服务实例。</para>
        /// </returns>
        public T ResolveKeyed<T>(object key, params Parameter[]? parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveKeyed<T>(key, parameters);
        }

        /// <summary>
        /// Begin a child resolver scope.
        /// </summary>
        public IResolver BeginResolverScope() => provider.CreateResolver(true);

        internal object CreateInstance(Type implementationType)
        {
            ArgumentNullException.ThrowIfNull(provider);
            ArgumentNullException.ThrowIfNull(implementationType);
            var constructor = ConstructorCache.GetOrAdd(implementationType, static type =>
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                return constructors.Length == 0 ? throw new InvalidOperationException($"No public constructor found for type '{type.FullName}'.") : constructors.OrderByDescending(c => c.GetParameters().Length).First();
            });
            var parameters = constructor.GetParameters().Select(parameter =>
            {
                var keyed = parameter.CustomAttributes.FirstOrDefault(c => c.AttributeType == typeof(FromKeyedServicesAttribute));
                if (keyed is not null)
                {
                    var key = keyed.ConstructorArguments.FirstOrDefault().Value;
                    return provider.GetRequiredKeyedService(parameter.ParameterType, key!);
                }
                var value = provider.GetService(parameter.ParameterType);
                return value ?? (parameter.HasDefaultValue ? parameter.DefaultValue : throw new InvalidOperationException($"Unable to resolve service for type '{parameter.ParameterType}' while attempting to activate '{implementationType}'."));
            }).ToArray();
            return constructor.Invoke(parameters);
        }
    }
}