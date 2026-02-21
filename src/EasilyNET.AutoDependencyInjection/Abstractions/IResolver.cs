namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// Lightweight resolver that provides dynamic resolution similar to Autofac while staying on top of Microsoft.Extensions.DependencyInjection.
/// <para>
/// For basic resolution without parameter overrides, prefer using <see cref="IServiceProvider" /> directly
/// (e.g. <c>provider.GetRequiredService&lt;T&gt;()</c>, <c>provider.GetKeyedService&lt;T&gt;(key)</c>).
/// </para>
/// <para>
/// The unique value of IResolver is constructor parameter overrides via <see cref="Parameter" /> —
/// use <see cref="Resolve{T}(Parameter[])" /> and <see cref="ResolveKeyed{T}(object, Parameter[])" /> for that.
/// </para>
/// </summary>
public interface IResolver : IDisposable
{
    /// <summary>Resolve with dynamic constructor parameter overrides.</summary>
    T Resolve<T>(params Parameter[] parameters);

    /// <summary>Resolve with dynamic constructor parameter overrides.</summary>
    object Resolve(Type serviceType, params Parameter[] parameters);

    /// <summary>Resolve named registration with optional parameter overrides.</summary>
    T ResolveNamed<T>(string name, params Parameter[]? parameters);

    /// <summary>Resolve keyed registration with optional parameter overrides.</summary>
    T ResolveKeyed<T>(object key, params Parameter[]? parameters);
}