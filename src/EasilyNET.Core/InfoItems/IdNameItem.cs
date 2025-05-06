// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
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
    public ReferenceItem GetReferenceItem() => new(Id, Name);

    /// <summary>
    ///     <para xml:lang="en">Get the Name value</para>
    ///     <para xml:lang="zh">获取Name值</para>
    /// </summary>
    public override string ToString() => Name;
}