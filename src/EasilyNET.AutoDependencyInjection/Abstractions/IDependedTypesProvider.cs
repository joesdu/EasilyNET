namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">Provider of dependent types</para>
///     <para xml:lang="zh">被依赖的类型提供方</para>
/// </summary>
internal interface IDependedTypesProvider
{
    /// <summary>
    ///     <para xml:lang="en">Get collection of dependent types</para>
    ///     <para xml:lang="zh">得到依赖类型集合</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">Collection of dependent types</para>
    ///     <para xml:lang="zh">依赖类型集合</para>
    /// </returns>
    IEnumerable<Type> GetDependedTypes();
}