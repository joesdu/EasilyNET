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
            if (!createScope)
            {
                return new Resolver(provider);
            }
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            return new Resolver(scope.ServiceProvider, scope);
        }

        /// <summary>
        /// Resolve type dynamically.
        /// </summary>
        public object Resolve(Type serviceType)
        {
            using var resolver = provider.CreateResolver();
            return resolver.Resolve(serviceType);
        }

        /// <summary>
        /// Resolve with constructor parameter overrides.
        /// </summary>
        public T Resolve<T>(params Parameter[] parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.Resolve<T>(parameters);
        }

        /// <summary>
        /// Resolve with constructor parameter overrides.
        /// </summary>
        public object Resolve(Type serviceType, params Parameter[] parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.Resolve(serviceType, parameters);
        }

        /// <summary>
        /// Resolve all registrations for <typeparamref name="T" />.
        /// </summary>
        public IEnumerable<T> ResolveAll<T>()
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveAll<T>();
        }

        /// <summary>
        /// Resolve optional service.
        /// </summary>
        public T? ResolveOptional<T>()
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveOptional<T>();
        }

        /// <summary>
        /// Try resolve service.
        /// </summary>
        public bool TryResolve<T>(out T? instance)
        {
            using var resolver = provider.CreateResolver();
            return resolver.TryResolve(out instance);
        }

        /// <summary>
        /// Try resolve service by type.
        /// </summary>
        public bool TryResolve(Type serviceType, out object? instance)
        {
            using var resolver = provider.CreateResolver();
            return resolver.TryResolve(serviceType, out instance);
        }

        /// <summary>
        /// Resolve named registration.
        /// </summary>
        public T ResolveNamed<T>(string name, params Parameter[]? parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveNamed<T>(name, parameters);
        }

        /// <summary>
        /// Resolve keyed registration.
        /// </summary>
        public T ResolveKeyed<T>(object key, params Parameter[]? parameters)
        {
            using var resolver = provider.CreateResolver();
            return resolver.ResolveKeyed<T>(key, parameters);
        }

        /// <summary>
        /// Begin a child resolver scope.
        /// <para>⚠️ The caller is responsible for disposing the returned IResolver.</para>
        /// </summary>
        public IResolver BeginResolverScope() => provider.CreateResolver(true);

        internal object CreateInstance(Type implementationType)
        {
            ArgumentNullException.ThrowIfNull(implementationType);
            var constructor = ConstructorCache.GetOrAdd(implementationType, static type =>
            {
                var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (constructors.Length == 0)
                {
                    throw new InvalidOperationException($"No public constructor found for type '{type.FullName}'.");
                }
                return constructors.OrderByDescending(c => c.GetParameters().Length).First();
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