using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EasilyNET.AutoDependencyInjection.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

internal sealed class Resolver(IServiceProvider provider, IServiceScope? scope = null) : IResolver
{
    private static readonly ConcurrentDictionary<Type, ConstructorInfo> CtorCache = new();

    public void Dispose()
    {
        scope?.Dispose();
        GC.SuppressFinalize(this);
    }

    public T Resolve<T>() => (T)Resolve(typeof(T));

    public object Resolve(Type serviceType)
    {
        var service = provider.GetService(serviceType) ?? throw new InvalidOperationException($"Unable to resolve service of type '{serviceType}'.");
        return service;
    }

    public T Resolve<T>(params Parameter[] parameters) => (T)Resolve(typeof(T), parameters);

    public object Resolve(Type serviceType, params Parameter[]? parameters)
    {
        if (parameters is null || parameters.Length == 0)
        {
            return Resolve(serviceType);
        }
        // create with parameter overrides
        var implType = GetImplementationType(serviceType) ?? serviceType;
        var ctor = SelectConstructor(implType, parameters, CanResolve);
        var args = BuildArguments(ctor, parameters);
        return ctor.Invoke(args);
    }

    public IEnumerable<T> ResolveAll<T>() => (IEnumerable<T>)provider.GetServices(typeof(T));

    public bool TryResolve<T>([MaybeNullWhen(false)] out T instance)
    {
        var ok = TryResolve(typeof(T), out var obj);
        instance = ok ? (T)obj! : default;
        return ok;
    }

    public bool TryResolve(Type serviceType, out object? instance)
    {
        instance = provider.GetService(serviceType);
        return instance is not null;
    }

    public T? ResolveOptional<T>() => (T?)provider.GetService(typeof(T));

    public T ResolveNamed<T>(string name, params Parameter[]? parameters) => ResolveKeyed<T>(name, parameters);

    public T ResolveKeyed<T>(object key, params Parameter[]? parameters)
    {
        try
        {
            if (parameters is null || parameters.Length == 0)
            {
                // use built-in keyed services if available
                var value = provider.GetRequiredKeyedService(typeof(T), key);
                return (T)value;
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            // fallback to our registry
        }
        var tupleKey = (key, typeof(T));
        if (!ServiceProviderExtension.NamedServices.TryGetValue(tupleKey, out var descriptor))
        {
            throw new InvalidOperationException($"No keyed service registered for key '{key}' and type '{typeof(T)}'.");
        }
        var ctor = SelectConstructor(descriptor.ImplementationType, parameters ?? [], CanResolve);
        var args = BuildArguments(ctor, parameters ?? []);
        return (T)ctor.Invoke(args);
    }

    public IResolver BeginScope()
    {
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var serviceScope = scopeFactory.CreateScope();
        return new Resolver(serviceScope.ServiceProvider, serviceScope);
    }

    ~Resolver()
    {
        Dispose();
    }

    private static ConstructorInfo SelectConstructor(Type implType, Parameter[] parameters, Func<Type, bool> canResolve)
    {
        if (!CtorCache.TryGetValue(implType, out var ctor))
        {
            ctor = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                           .OrderByDescending(c => c.GetParameters().Length)
                           .FirstOrDefault() ??
                   throw new InvalidOperationException($"No public constructor found for type {implType}.");
            CtorCache[implType] = ctor;
        }
        // prefer constructor where all parameters can be satisfied
        var candidates = implType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                                 .OrderByDescending(c => c.GetParameters().Length)
                                 .ToArray();
        foreach (var c in candidates)
        {
            if (c.GetParameters().All(p => parameters.Any(prm => prm.CanSupplyValue(p.ParameterType, p.Name)) || canResolve(p.ParameterType)))
            {
                return c;
            }
        }
        return ctor;
    }

    private object?[] BuildArguments(ConstructorInfo ctor, Parameter[] parameters)
    {
        return
        [
            .. ctor.GetParameters().Select(p =>
            {
                var match = parameters.FirstOrDefault(prm => prm.CanSupplyValue(p.ParameterType, p.Name));
                if (match is not null)
                {
                    return match.GetValue(provider, p.ParameterType, p.Name);
                }
                // handle keyed parameter attribute
                var keyed = p.GetCustomAttributes<FromKeyedServicesAttribute>().FirstOrDefault();
                if (keyed is null)
                {
                    return provider.GetRequiredService(p.ParameterType);
                }
                var keyProp = keyed.GetType().GetProperty(nameof(FromKeyedServicesAttribute.Key));
                var key = keyProp?.GetValue(keyed)!;
                return provider.GetRequiredKeyedService(p.ParameterType, key);
            })
        ];
    }

    private bool CanResolve(Type t) => provider.GetService(t) is not null;

    private static Type? GetImplementationType(Type serviceType) =>
        // Best-effort: if serviceType is concrete just return it, otherwise cannot know implementation from provider.
        serviceType is { IsAbstract: false, IsInterface: false } ? serviceType : null;
}