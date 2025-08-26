using System.Diagnostics.CodeAnalysis;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// Lightweight resolver that provides dynamic resolution similar to Autofac while staying on top of Microsoft.Extensions.DependencyInjection.
/// </summary>
public interface IResolver : IDisposable
{
    /// <summary>Resolve service or throw if missing.</summary>
    T Resolve<T>();

    /// <summary>Resolve service by type or throw if missing.</summary>
    object Resolve(Type serviceType);

    /// <summary>Resolve with dynamic constructor parameter overrides.</summary>
    T Resolve<T>(params Parameter[] parameters);

    /// <summary>Resolve with dynamic constructor parameter overrides.</summary>
    object Resolve(Type serviceType, params Parameter[] parameters);

    /// <summary>Resolve all registrations of a service.</summary>
    IEnumerable<T> ResolveAll<T>();

    /// <summary>Try resolve service.</summary>
    bool TryResolve<T>([MaybeNullWhen(false)] out T instance);

    /// <summary>Try resolve service by type.</summary>
    bool TryResolve(Type serviceType, out object? instance);

    /// <summary>Resolve if present, otherwise return default.</summary>
    T? ResolveOptional<T>();

    /// <summary>Resolve named registration.</summary>
    T ResolveNamed<T>(string name, params Parameter[]? parameters);

    /// <summary>Resolve keyed registration.</summary>
    T ResolveKeyed<T>(object key, params Parameter[]? parameters);

    /// <summary>Create a child scope.</summary>
    IResolver BeginScope();
}