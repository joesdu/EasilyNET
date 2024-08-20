using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Abstractions.Clause;

namespace EasilyNET.ExpressMapper.Configuration;

/// <summary>
/// Implementation of the configuration with source and destination types.
/// 具有源类型和目标类型的配置实现。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
/// <param name="clauses">The collection of clauses. 子句集合。</param>
public class Config<TSource, TDest>(IEnumerable<IClause<TSource, TDest>> clauses) : IConfig<TSource, TDest>
{
    /// <summary>
    /// Gets the collection of clauses.
    /// 获取子句集合。
    /// </summary>
    public IEnumerable<IClause<TSource, TDest>> Clauses { get; } = clauses;

    /// <summary>
    /// Gets the mapping key.
    /// 获取映射键。
    /// </summary>
    public MapKey Key => MapKey.Form<TSource, TDest>();
}