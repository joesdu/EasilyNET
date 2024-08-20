using System.Reflection;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Abstractions.Clause;

/// <summary>
/// Interface for constructor clause.
/// 构造函数子句的接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IConstructClause<TSource, TDest> : IClause<TSource, TDest>
{
    /// <summary>
    /// Gets the constructor information.
    /// 获取构造函数信息。
    /// </summary>
    public ConstructorInfo? ConstructorInfo { get; }

    /// <summary>
    /// Gets the constructor parameters.
    /// 获取构造函数参数。
    /// </summary>
    public IEnumerable<MappingMember> ConstructorParams { get; }
}