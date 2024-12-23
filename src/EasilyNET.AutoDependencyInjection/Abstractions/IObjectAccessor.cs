namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">Object accessor</para>
///     <para xml:lang="zh">对象存取器</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">Type of the object</para>
///     <para xml:lang="zh">对象的类型</para>
/// </typeparam>
internal interface IObjectAccessor<T>
{
    /// <summary>
    ///     <para xml:lang="en">Value</para>
    ///     <para xml:lang="zh">值</para>
    /// </summary>
    internal T? Value { get; set; }
}