namespace EasilyNET.ExpressMapper.Abstractions.Clause;

/// <summary>
/// Interface for a mapping clause.
/// 映射子句的接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IClause<TSource, TDest>
{
    /// <summary>
    /// Gets a value indicating whether this clause is valid.
    /// 获取一个值，该值指示此子句是否有效。
    /// </summary>
    bool IsValidClause { get; }
}

/// <summary>
/// Interface for a reversible mapping clause.
/// 可逆映射子句的接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IReverseAbleClause<TSource, TDest> : IClause<TSource, TDest>
{
    /// <summary>
    /// Gets the reverse clause.
    /// 获取反向子句。
    /// </summary>
    /// <returns>The reverse clause. 反向子句。</returns>
    public IClause<TDest, TSource> GetReverseClause();
}