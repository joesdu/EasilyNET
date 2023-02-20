namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// 被依赖的类型提供方
/// </summary>
internal interface IDependedTypesProvider
{
    /// <summary>
    /// 得到依赖类型集合
    /// </summary>
    /// <returns></returns>
    IEnumerable<Type> GetDependedTypes();
}