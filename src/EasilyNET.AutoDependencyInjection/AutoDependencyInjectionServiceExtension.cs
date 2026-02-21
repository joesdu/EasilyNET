using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Factories;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en"><see cref="IServiceCollection" /> extensions</para>
///     <para xml:lang="zh"><see cref="IServiceCollection" /> 扩展</para>
/// </summary>
public static class AutoDependencyInjectionServiceExtension
{
    /// <summary>
    ///     <para xml:lang="en">Get the application host</para>
    ///     <para xml:lang="zh">获取应用程序构建器</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    public static IHost GetApplicationHost(this ApplicationContext context) => context.ServiceProvider.GetRequiredService<IObjectAccessor<IHost>>().Value ?? throw new ArgumentNullException(nameof(context));

    /// <param name="provider">
    ///     <para xml:lang="en">Service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    extension(IServiceProvider provider)
    {
        /// <summary>
        ///     <para xml:lang="en">Get the <see cref="IConfiguration" /> service</para>
        ///     <para xml:lang="zh">获取 <see cref="IConfiguration" /> 服务</para>
        /// </summary>
        public IConfiguration GetConfiguration() => provider.GetRequiredService<IConfiguration>();
    }

    /// <param name="host">
    ///     <para xml:lang="en">The application host</para>
    ///     <para xml:lang="zh">应用程序构建器</para>
    /// </param>
    extension(IHost host)
    {
        /// <summary>
        ///     <para xml:lang="en">Initialize the application and configure middleware</para>
        ///     <para xml:lang="zh">初始化应用，配置中间件</para>
        /// </summary>
        public IHost InitializeApplication()
        {
            host.Services.GetRequiredService<IObjectAccessor<IHost>>().Value = host;
            var runner = host.Services.GetRequiredService<IStartupModuleRunner>();
            runner.Initialize(host.Services);
            return host;
        }

        /// <summary>
        ///     <para xml:lang="en">Initialize the application and configure middleware asynchronously</para>
        ///     <para xml:lang="zh">异步初始化应用，配置中间件</para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation token</para>
        ///     <para xml:lang="zh">取消令牌</para>
        /// </param>
        public async Task<IHost> InitializeApplicationAsync(CancellationToken cancellationToken = default)
        {
            host.Services.GetRequiredService<IObjectAccessor<IHost>>().Value = host;
            var runner = host.Services.GetRequiredService<IStartupModuleRunner>();
            await runner.InitializeAsync(host.Services, cancellationToken).ConfigureAwait(false);
            return host;
        }

        /// <summary>
        ///     <para xml:lang="en">Shutdown all modules (call during application stopping)</para>
        ///     <para xml:lang="zh">关闭所有模块（在应用停止时调用）</para>
        /// </summary>
        public void ShutdownApplication()
        {
            var runner = host.Services.GetService<IStartupModuleRunner>();
            runner?.Shutdown(host.Services);
        }

        /// <summary>
        ///     <para xml:lang="en">Shutdown all modules asynchronously (call during application stopping)</para>
        ///     <para xml:lang="zh">异步关闭所有模块（在应用停止时调用）</para>
        /// </summary>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation token</para>
        ///     <para xml:lang="zh">取消令牌</para>
        /// </param>
        public async Task ShutdownApplicationAsync(CancellationToken cancellationToken = default)
        {
            var runner = host.Services.GetService<IStartupModuleRunner>();
            if (runner is not null)
            {
                await runner.ShutdownAsync(host.Services, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    /// <param name="services">
    ///     <para xml:lang="en"><see cref="IServiceCollection" /> to configure services</para>
    ///     <para xml:lang="zh">用于配置服务的 <see cref="IServiceCollection" /></para>
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">Inject services</para>
        ///     <para xml:lang="zh">注入服务</para>
        /// </summary>
        /// <typeparam name="T">
        ///     <para xml:lang="en">Type of the application module</para>
        ///     <para xml:lang="zh">应用模块的类型</para>
        /// </typeparam>
        public IServiceCollection AddApplicationModules<T>() where T : AppModule
        {
            ArgumentNullException.ThrowIfNull(services);
            // 确保 ServiceRegistry 首先被注册
            _ = services.GetOrCreateRegistry();
            services.AddSingleton<IObjectAccessor<IHost>>(new ObjectAccessor<IHost>());
            services.AddScoped<IResolver>(sp => new Resolver(sp, sp.GetRequiredService<ServiceRegistry>()));
            services.AddSingleton(typeof(INamedServiceFactory<>), typeof(NamedServiceFactory<>));
            // Autofac-style implicit relationship types
            services.AddTransient(typeof(IIndex<,>), typeof(KeyedServiceIndex<,>));
            // Register module diagnostics
            services.AddSingleton<IModuleDiagnostics>(sp =>
                new ModuleDiagnostics(sp.GetRequiredService<IStartupModuleRunner>(), sp.GetRequiredService<ServiceRegistry>()));
            ApplicationFactory.Create<T>(services);
            return services;
        }

        // ReSharper disable once UnusedMethodReturnValue.Global
        internal IServiceCollection AddNamedService(Type serviceType, object key, Type implementationType, ServiceLifetime lifetime)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(serviceType);
            ArgumentNullException.ThrowIfNull(implementationType);
            var registry = services.GetOrCreateRegistry();
            registry.RegisterNamedService(key, serviceType, implementationType, lifetime);
            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    services.AddKeyedSingleton(serviceType, key, (p, _) => p.CreateInstance(implementationType));
                    break;
                case ServiceLifetime.Scoped:
                    services.AddKeyedScoped(serviceType, key, (p, _) => p.CreateInstance(implementationType));
                    break;
                case ServiceLifetime.Transient:
                    services.AddKeyedTransient(serviceType, key, (p, _) => p.CreateInstance(implementationType));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, null);
            }
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">Get or create the <see cref="ServiceRegistry" /> for the service collection</para>
        ///     <para xml:lang="zh">获取或创建服务集合的 <see cref="ServiceRegistry" /></para>
        /// </summary>
        internal ServiceRegistry GetOrCreateRegistry()
        {
            // Fast path: try to get an existing registry without locking.
            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ServiceRegistry));
            if (descriptor?.ImplementationInstance is ServiceRegistry existing)
            {
                return existing;
            }
            // Slow path: ensure only a single ServiceRegistry is created in concurrent scenarios.
            lock (services)
            {
                // Double-check after acquiring the lock in case another thread already registered it.
                descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(ServiceRegistry));
                if (descriptor?.ImplementationInstance is ServiceRegistry existingAfterLock)
                {
                    return existingAfterLock;
                }
                var registry = new ServiceRegistry();
                services.AddSingleton(registry);
                return registry;
            }
        }
    }
}