using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 映射子句规则类，用于定义目标成员和源表达式之间的映射规则。
/// Class for map clause rules, used to define mapping rules between destination members and source expressions.
/// </summary>
public class MapClauseRule : IMappingRule
{
    /// <summary>
    /// 目标成员。
    /// The destination member.
    /// </summary>
    public required MappingMember DestinationMember { get; init; }

    /// <summary>
    /// 源表达式。
    /// The source lambda expression.
    /// </summary>
    public required LambdaExpression SourceLambda { get; init; }
}