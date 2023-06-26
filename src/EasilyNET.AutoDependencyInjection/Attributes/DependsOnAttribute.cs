using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Attributes;

/// <summary>
/// DependsOnAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependsOnAttribute(params Type[] dependedTypes) : Attribute, IDependedTypesProvider
{
    /// <summary>
    /// 得到依赖类型集合
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Type> GetDependedTypes() => dependedTypes;
}