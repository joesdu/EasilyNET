using System.Collections.Concurrent;
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
    private static readonly ConcurrentDictionary<Type, ObjectFactory> FactoryCache = [];

    /// <param name="provider">Service provider.</param>
    extension(IServiceProvider provider)
    {
        /// <summary>
        ///     <para xml:lang="en">Whether to create an isolated scope for the resolver.</para>
        ///     <para xml:lang="zh">是否为解析器创建一个独立的作用域</para>
        /// </summary>
        public IResolver CreateResolver(bool createScope = false)
        {
            var registry = provider.GetService<ServiceRegistry>();
            if (!createScope)
            {
                return new Resolver(provider, registry);
            }
            var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
            var scope = scopeFactory.CreateScope();
            return new Resolver(scope.ServiceProvider, registry, scope);
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
            using var resolver = provider.CreateResolver();
            return resolver.Resolve<T>(parameters);
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
            using var resolver = provider.CreateResolver();
            return resolver.Resolve(serviceType, parameters);
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
        ///     <para xml:lang="en">
        ///     Resolve a service with controlled lifetime, similar to Autofac's <c>Owned&lt;T&gt;</c>.
        ///     The service is resolved in an isolated scope; disposing the returned <see cref="Owned{T}" />
        ///     releases the scope and all scoped dependencies.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     解析具有受控生命周期的服务，类似于 Autofac 的 <c>Owned&lt;T&gt;</c>。
        ///     服务在独立作用域中解析；释放返回的 <see cref="Owned{T}" /> 会释放该作用域及所有 Scoped 依赖。
        ///     </para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">The service type to resolve.</para>
        ///     <para xml:lang="zh">要解析的服务类型。</para>
        /// </typeparam>
        /// <returns>
        ///     <para xml:lang="en">An <see cref="Owned{T}" /> wrapping the resolved service. The caller must dispose it.</para>
        ///     <para xml:lang="zh">包装已解析服务的 <see cref="Owned{T}" />。调用者必须释放它。</para>
        /// </returns>
        public Owned<T> ResolveOwned<T>() where T : notnull => OwnedFactory.Create<T>(provider);

        internal object CreateInstance(Type implementationType)
        {
            ArgumentNullException.ThrowIfNull(implementationType);
            var factory = FactoryCache.GetOrAdd(implementationType,
                static t => ActivatorUtilities.CreateFactory(t, Type.EmptyTypes));
            return factory(provider, null);
        }
    }
}