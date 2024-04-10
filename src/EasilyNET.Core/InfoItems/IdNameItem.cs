// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Core;

/// <summary>
/// 包含Id和Name字段的对象
/// </summary>
// ReSharper disable once UnusedType.Global
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
    /// 转化成 <see cref="ReferenceItem" /> 对象
    /// </summary>
    /// <returns>
    ///     <see cref="ReferenceItem" />
    /// </returns>
    public ReferenceItem GetReferenceItem() => new(Id, Name);

    /// <summary>
    /// 获取Name值
    /// </summary>
    /// <returns></returns>
    public override string ToString() => Name;
}