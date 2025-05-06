using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Attributes;

/// <inheritdoc cref="IDependedTypesProvider" />
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependsOnAttribute(params Type[] dependedTypes) : Attribute, IDependedTypesProvider
{
    /// <summary>
    ///     <para xml:lang="en">Get collection of dependent types</para>
    ///     <para xml:lang="zh">得到依赖类型集合</para>
    /// </summary>
    public IEnumerable<Type> GetDependedTypes() => dependedTypes;
}