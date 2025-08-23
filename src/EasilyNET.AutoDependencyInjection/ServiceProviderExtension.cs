using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.AutoDependencyInjection;
using EasilyNET.AutoDependencyInjection.Abstractions;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en"><see cref="IServiceProvider" /> extensions</para>
///     <para xml:lang="zh"><see cref="IServiceProvider" /> 扩展</para>
/// </summary>
public static class ServiceProviderExtension
{
    // Keyed by (key, service type) to avoid collisions when the same key is used for multiple service types
    internal static readonly ConcurrentDictionary<(object Key, Type ServiceType), NamedServiceDescriptor> NamedServices = new();

    private static readonly ConcurrentDictionary<Type, ConstructorInfo> ConstructorCache = new();

    internal static object CreateInstance(this IServiceProvider provider, Type implementationType)
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

    // Dynamic resolve helpers

    public static IResolver CreateResolver(this IServiceProvider provider) => new Resolver(provider);

    public static T Resolve<T>(this IServiceProvider provider) => provider.CreateResolver().Resolve<T>();

    public static object Resolve(this IServiceProvider provider, Type serviceType) => provider.CreateResolver().Resolve(serviceType);

    public static T? ResolveOptional<T>(this IServiceProvider provider) => provider.CreateResolver().ResolveOptional<T>();

    public static bool TryResolve<T>(this IServiceProvider provider, out T instance) => provider.CreateResolver().TryResolve(out instance);

    public static bool TryResolve(this IServiceProvider provider, Type serviceType, out object? instance) => provider.CreateResolver().TryResolve(serviceType, out instance);

    // Keep only dictionary overloads to avoid ambiguity with target-typed new()
    public static T ResolveNamed<T>(this IServiceProvider provider, string name, Dictionary<string, object?>? parameters) => provider.CreateResolver().ResolveKeyed<T>(name, ToParameters(parameters));

    public static T ResolveKeyed<T>(this IServiceProvider provider, object key, Dictionary<string, object?>? parameters) => provider.CreateResolver().ResolveKeyed<T>(key, ToParameters(parameters));

    private static Parameter[] ToParameters(Dictionary<string, object?>? dict)
    {
        if (dict is null || dict.Count == 0)
            return [];
        var arr = new Parameter[dict.Count];
        var i = 0;
        foreach (var kv in dict)
        {
            arr[i++] = new NamedParameter(kv.Key, kv.Value);
        }
        return arr;
    }
}