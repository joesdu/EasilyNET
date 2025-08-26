/*
 * 装饰器扩展（DecorationExtensions）使用说明
 * ------------------------------------------------
 * 该扩展用于在依赖注入容器中批量为某一服务类型添加装饰器（Decorator），
 * 适用于日志、缓存、权限、异常处理等横切关注点的解耦与复用。
 *
 * 典型使用场景：
 * 1. 定义服务接口与实现：
 *    public interface IFooService { void DoWork(); }
 *    public class FooService : IFooService { public void DoWork() { ... } }
 *
 * 2. 定义装饰器（需实现同一接口，并在构造函数中注入被装饰服务）：
 *    public class FooServiceLoggingDecorator : IFooService
 *    {
 *        private readonly IFooService _inner;
 *        private readonly ILogger<FooServiceLoggingDecorator> _logger;
 *        public FooServiceLoggingDecorator(IFooService inner, ILogger<FooServiceLoggingDecorator> logger)
 *        {
 *            _inner = inner;
 *            _logger = logger;
 *        }
 *        public void DoWork()
 *        {
 *            _logger.LogInformation("Before DoWork");
 *            _inner.DoWork();
 *            _logger.LogInformation("After DoWork");
 *        }
 *    }
 *
 * 3. 注册服务与应用装饰器：
 *    services.AddTransient<IFooService, FooService>();
 *    services.Decorate<IFooService, FooServiceLoggingDecorator>();
 *
 * 4. 结果：
 *    通过依赖注入获取 IFooService 时，实际获得的是装饰器实例，
 *    装饰器会自动调用原始服务并附加横切逻辑。
 *
 * 注意事项：
 * - 装饰器必须实现被装饰的服务接口，并在构造函数中接收该接口类型参数。
 * - 支持多种注册方式（类型、工厂、实例），支持依赖注入特性（如 FromKeyedServices）。
 * - 可多次调用 Decorate 方法形成装饰器链。
 */

using System.Collections.Concurrent;
using System.Reflection;

// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to decorate registered services with decorators (classic decorator pattern).
/// </summary>
public static class DecorationExtensions
{
    private static readonly ConcurrentDictionary<(Type Decorator, Type Service), ConstructorInfo> DecoratorCtorCache = new();

    /// <summary>
    /// Decorate all registrations of TService with TDecorator. TDecorator must implement/derive from TService
    /// and have a public constructor that takes an argument assignable from TService.
    /// </summary>
    public static IServiceCollection Decorate<TService, TDecorator>(this IServiceCollection services)
        where TDecorator : TService
    {
        ArgumentNullException.ThrowIfNull(services);
        var serviceType = typeof(TService);
        var decoratorType = typeof(TDecorator);
        for (var i = 0; i < services.Count; i++)
        {
            var descriptor = services[i];
            if (descriptor.ServiceType != serviceType)
            {
                continue;
            }
            var newDescriptor = descriptor switch
            {
                { ImplementationFactory: not null }  => CreateFromFactory(descriptor, serviceType, decoratorType),
                { ImplementationInstance: not null } => CreateFromInstance(descriptor, serviceType, decoratorType),
                _                                    => CreateFromType(descriptor, serviceType, decoratorType)
            };
            services[i] = newDescriptor;
        }
        return services;
    }

    private static ServiceDescriptor CreateFromType(ServiceDescriptor descriptor, Type serviceType, Type decoratorType)
    {
        return ServiceDescriptor.Describe(serviceType,
            provider =>
            {
                var inner = CreateInnerFromType(provider, descriptor);
                return CreateDecorator(provider, decoratorType, serviceType, inner);
            },
            descriptor.Lifetime);
    }

    private static ServiceDescriptor CreateFromFactory(ServiceDescriptor descriptor, Type serviceType, Type decoratorType)
    {
        return ServiceDescriptor.Describe(serviceType,
            provider =>
            {
                var factory = descriptor.ImplementationFactory ?? throw new InvalidOperationException("ImplementationFactory is null.");
                var inner = factory(provider);
                return CreateDecorator(provider, decoratorType, serviceType, inner);
            },
            descriptor.Lifetime);
    }

    private static ServiceDescriptor CreateFromInstance(ServiceDescriptor descriptor, Type serviceType, Type decoratorType)
    {
        if (descriptor.Lifetime != ServiceLifetime.Singleton)
        {
            // ImplementationInstance implies singleton; if not, fall back to factory preserving lifetime.
            return CreateFromFactory(descriptor, serviceType, decoratorType);
        }
        IServiceProvider? rootProvider = null;
        var lazy = new Lazy<object?>(() => CreateDecorator(rootProvider!, decoratorType, serviceType, descriptor.ImplementationInstance!), true);
        return ServiceDescriptor.Singleton(serviceType, sp =>
        {
            // capture once
            rootProvider ??= sp;
            return lazy.Value!;
        });
    }

    private static object CreateInnerFromType(IServiceProvider provider, ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is null)
        {
            // This path should not occur here, guarded by caller, but keep safe.
            return descriptor.ImplementationFactory?.Invoke(provider) ?? descriptor.ImplementationInstance!;
        }
        // Create original instance honoring constructor parameter attributes
        var implType = descriptor.ImplementationType;
        var ctor = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault() ??
                   throw new InvalidOperationException($"No public constructor found for type {implType}.");
        var args = ctor.GetParameters().Select(p =>
        {
            var keyed = p.GetCustomAttributes<FromKeyedServicesAttribute>().FirstOrDefault();
            if (keyed is null)
            {
                return provider.GetRequiredService(p.ParameterType);
            }
            var keyProp = keyed.GetType().GetProperty(nameof(FromKeyedServicesAttribute.Key));
            var key = keyProp?.GetValue(keyed)!;
            return provider.GetRequiredKeyedService(p.ParameterType, key);
        }).ToArray();
        return ctor.Invoke(args);
    }

    private static object CreateDecorator(IServiceProvider provider, Type decoratorType, Type serviceType, object inner)
    {
        var key = (decoratorType, serviceType);
        var ctor = DecoratorCtorCache.GetOrAdd(key, k =>
        {
            var ctors = k.Decorator.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                         .OrderByDescending(c => c.GetParameters().Length)
                         .ToArray();
            foreach (var c in ctors)
            {
                var parameters = c.GetParameters();
                if (parameters.Any(p => p.ParameterType.IsAssignableTo(k.Service)))
                {
                    return c;
                }
            }
            throw new InvalidOperationException($"No suitable constructor found on decorator {k.Decorator} that accepts service type {k.Service}.");
        });
        var args = ctor.GetParameters().Select(p =>
        {
            if (p.ParameterType.IsAssignableTo(serviceType))
            {
                return inner;
            }
            var keyed = p.GetCustomAttributes<FromKeyedServicesAttribute>().FirstOrDefault();
            if (keyed is null)
            {
                return provider.GetRequiredService(p.ParameterType);
            }
            var keyProp = keyed.GetType().GetProperty(nameof(FromKeyedServicesAttribute.Key));
            var keyVal = keyProp?.GetValue(keyed)!;
            return provider.GetRequiredKeyedService(p.ParameterType, keyVal);
        }).ToArray();
        return ctor.Invoke(args);
    }
}