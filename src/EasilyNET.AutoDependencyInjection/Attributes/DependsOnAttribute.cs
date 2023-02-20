using EasilyNET.AutoDependencyInjection.Abstractions;

// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection.Attributes;

/// <summary>
/// DependsOnAttribute
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class DependsOnAttribute : Attribute, IDependedTypesProvider
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="dependedTypes"></param>
    public DependsOnAttribute(params Type[] dependedTypes) => DependedTypes = dependedTypes;
    /// <summary>
    /// 依赖类型集合
    /// </summary>
    private Type[] DependedTypes { get; }
    /// <summary>
    /// 得到依赖类型集合
    /// </summary>
    /// <returns></returns>
    public IEnumerable<Type> GetDependedTypes() => DependedTypes;
}