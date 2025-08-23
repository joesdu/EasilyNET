using System.Collections.Concurrent;
using System.Reflection;

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
                var inner = descriptor.ImplementationFactory!(provider)!;
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
        var lazy = new Lazy<object?>(() => CreateDecorator(rootProvider!, decoratorType, serviceType, descriptor.ImplementationInstance!),
            true);
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
            var keyed = p.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "FromKeyedServicesAttribute");
            if (keyed is null)
            {
                return provider.GetRequiredService(p.ParameterType);
            }
            var keyProp = keyed.GetType().GetProperty("Key");
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
            var keyed = p.GetCustomAttributes().FirstOrDefault(a => a.GetType().Name == "FromKeyedServicesAttribute");
            if (keyed is null)
            {
                return provider.GetRequiredService(p.ParameterType);
            }
            var keyProp = keyed.GetType().GetProperty("Key");
            var keyVal = keyProp?.GetValue(keyed)!;
            return provider.GetRequiredKeyedService(p.ParameterType, keyVal);
        }).ToArray();
        return ctor.Invoke(args);
    }
}