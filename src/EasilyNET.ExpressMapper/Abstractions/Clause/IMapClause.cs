using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Abstractions.Clause;

/// <summary>
/// Interface for map clause.
/// 映射子句的接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IMapClause<TSource, TDest> : IClause<TSource, TDest>
{
    /// <summary>
    /// Gets the destination member.
    /// 获取目标成员。
    /// </summary>
    public MappingMember? DestinationMember { get; }

    /// <summary>
    /// Gets the lambda expression.
    /// 获取 lambda 表达式。
    /// </summary>
    public LambdaExpression? Expression { get; }
}