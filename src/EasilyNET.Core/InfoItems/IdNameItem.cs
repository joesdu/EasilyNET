// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core;

/// <summary>
/// 包含Id和Name字段的对象
/// </summary>
public class IdNameItem
{
    /// <summary>
    /// ID
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 转化成ReferenceItem对象
    /// </summary>
    /// <returns>ReferenceItem</returns>
    public ReferenceItem GetReferenceItem() => new(Id, Name);

    /// <summary>
    /// 获取Name值
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;
}