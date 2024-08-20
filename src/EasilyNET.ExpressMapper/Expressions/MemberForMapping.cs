using System.Reflection;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 用于映射的成员类，表示映射中的成员信息。
/// Class for member for mapping, representing member information in mappings.
/// </summary>
public class MemberForMapping
{
    /// <summary>
    /// 成员名称。
    /// The name of the member.
    /// </summary>
    public required string MemberName { get; init; }

    /// <summary>
    /// 成员信息。
    /// The member information.
    /// </summary>
    public required MemberInfo MemberInfo { get; init; }

    /// <summary>
    /// 成员类型。
    /// The type of the member.
    /// </summary>
    public required Type MemberType { get; init; }
}