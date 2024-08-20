using System.Reflection;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 映射成员记录，用于表示映射中的成员信息。
/// Record for mapping members, used to represent member information in mappings.
/// </summary>
public record MappingMember
{
    /// <summary>
    /// 成员名称。
    /// The name of the member.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// 成员类型。
    /// The type of the member.
    /// </summary>
    public required Type Type { get; init; }

    /// <summary>
    /// 成员信息。
    /// The member information.
    /// </summary>
    public required MemberInfo Info { get; init; }
}