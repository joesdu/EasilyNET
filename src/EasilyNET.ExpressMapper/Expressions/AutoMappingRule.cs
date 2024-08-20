using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 自动映射规则类，用于定义源成员和目标成员之间的映射规则。
/// Class for auto-mapping rules, used to define mapping rules between source and destination members.
/// </summary>
public class AutoMappingRule : IMappingRule
{
    /// <summary>
    /// 源成员。
    /// The source member.
    /// </summary>
    public required MappingMember SourceMember { get; init; }

    /// <summary>
    /// 目标成员。
    /// The destination member.
    /// </summary>
    public required MappingMember DestinationMember { get; init; }
}