using EasilyNET.ExpressMapper.Abstractions.Clause;

namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// Interface for configuration that includes key keeping functionality.
/// 包含密钥保持功能的配置接口。
/// </summary>
public interface IConfig : IKeyKeeper;

/// <summary>
/// Interface for configuration with source and destination types.
/// 具有源类型和目标类型的配置接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IConfig<TSource, TDest> : IConfig
{
    /// <summary>
    /// Gets the collection of clauses.
    /// 获取子句集合。
    /// </summary>
    IEnumerable<IClause<TSource, TDest>> Clauses { get; }
}