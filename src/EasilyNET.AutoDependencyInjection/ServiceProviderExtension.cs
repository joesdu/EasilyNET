using EasilyNET.AutoDependencyInjection;
using EasilyNET.Core.Misc;

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en"><see cref="IServiceProvider" /> extensions</para>
///     <para xml:lang="zh"><see cref="IServiceProvider" /> 扩展</para>
/// </summary>
public static class ServiceProviderExtension
{
    internal static readonly Dictionary<string, NamedServiceDescriptor> NamedServices = [];

    internal static object CreateInstance(this IServiceProvider provider, Type implementationType)
    {
        // 在这里可以根据需要从provider获取依赖项并传入构造函数
        // Here you can get dependencies from the provider as needed and pass them into the constructor
        var constructor = implementationType.GetConstructors().FirstOrDefault();
        if (constructor is null)
        {
            throw new InvalidOperationException($"No public constructor found for type {implementationType.Name}");
        }
        var parameters = constructor.GetParameters().Select(p => provider.GetService(p.ParameterType)).ToArray();
        return constructor.Invoke(parameters);
    }

    /// <summary>
    ///     <para xml:lang="en">Resolve a named service using a non-default constructor</para>
    ///     <para xml:lang="zh">通过非默认构造函数获取命名服务</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">Type of the service</para>
    ///     <para xml:lang="zh">服务的类型</para>
    /// </typeparam>
    /// <param name="provider">
    ///     <para xml:lang="en">Service provider</para>
    ///     <para xml:lang="zh">服务提供者</para>
    /// </param>
    /// <param name="name">
    ///     <para xml:lang="en">Name of the service</para>
    ///     <para xml:lang="zh">服务的名称</para>
    /// </param>
    /// <param name="parameters">
    ///     <para xml:lang="en">Parameters to pass to the constructor</para>
    ///     <para xml:lang="zh">传递给构造函数的参数</para>
    /// </param>
    /// <exception cref="InvalidOperationException">
    ///     <para xml:lang="en">Thrown when no matching service or constructor is found</para>
    ///     <para xml:lang="zh">当找不到匹配的服务或构造函数时抛出</para>
    /// </exception>
    public static T ResolveNamed<T>(this IServiceProvider provider, string name, Dictionary<string, object?>? parameters = null)
    {
        if (!NamedServices.TryGetValue(name, out var descriptor))
        {
            throw new InvalidOperationException($"No service of type {typeof(T).Name} with name {name} found.");
        }
        var constructor = descriptor.ImplementationType.GetConstructors()
                                    .FirstOrDefault(c => c.GetParameters()
                                                          .All(p => parameters?.ContainsKey(p.Name.AsNotNull()) is true ||
                                                                    provider.GetService(p.ParameterType) is not null));
        if (constructor is null)
        {
            throw new InvalidOperationException($"No matching constructor found for type {descriptor.ImplementationType.Name} with provided parameters.");
        }
        var args = constructor.GetParameters()
                              .Select(p => parameters?.TryGetValue(p.Name.AsNotNull(), out var value) is true ? value : provider.GetService(p.ParameterType))
                              .ToArray();
        return (T)constructor.Invoke(args);
    }
}