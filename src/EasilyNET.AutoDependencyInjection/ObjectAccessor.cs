using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
///     <para xml:lang="en">Default implementation of <see cref="IObjectAccessor{T}" /></para>
///     <para xml:lang="zh"><see cref="IObjectAccessor{T}" /> 的默认实现</para>
/// </summary>
/// <typeparam name="T">
///     <para xml:lang="en">The type of the object being accessed</para>
///     <para xml:lang="zh">被访问对象的类型</para>
/// </typeparam>
internal sealed class ObjectAccessor<T> : IObjectAccessor<T>
{
    /// <inheritdoc />
    public T? Value { get; set; }
}