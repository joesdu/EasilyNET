// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Core;

/// <summary>
///     <para xml:lang="en">Object containing Id and Name fields</para>
///     <para xml:lang="zh">包含Id和Name字段的对象</para>
/// </summary>
// ReSharper disable once UnusedType.Global
public class IdNameItem
{
    /// <summary>
    ///     <para xml:lang="en">ID</para>
    ///     <para xml:lang="zh">ID</para>
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Name</para>
    ///     <para xml:lang="zh">Name</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Convert to <see cref="ReferenceItem" /> object</para>
    ///     <para xml:lang="zh">转化成 <see cref="ReferenceItem" /> 对象</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">
    ///         <see cref="ReferenceItem" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="ReferenceItem" />
    ///     </para>
    /// </returns>
    public ReferenceItem GetReferenceItem() => new(Id, Name);

    /// <summary>
    ///     <para xml:lang="en">Get the Name value</para>
    ///     <para xml:lang="zh">获取Name值</para>
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;
}