using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Abstractions.Clause;

/// <summary>
/// Interface for ignore clause.
/// 忽略子句的接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IIgnoreClause<TSource, TDest> : IClause<TSource, TDest>
{
    /// <summary>
    /// Gets the source ignore member.
    /// 获取源忽略成员。
    /// </summary>
    public MappingMember? SourceIgnoreMember { get; }

    /// <summary>
    /// Gets the destination ignore member.
    /// 获取目标忽略成员。
    /// </summary>
    public MappingMember? DestinationIgnoreMember { get; }
}