using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Expressions;

namespace EasilyNET.ExpressMapper.Configuration.Config.Clause;

/// <summary>
/// Helper class for handling mapping clauses.
/// 用于处理映射子句的辅助类。
/// </summary>
public static class ClauseHelper
{
    /// <summary>
    /// Gets the destination ignore members from the configuration.
    /// 从配置中获取目标忽略成员。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <param name="config">The configuration. 配置。</param>
    /// <returns>A collection of destination ignore members. 目标忽略成员的集合。</returns>
    public static IEnumerable<MappingMember> GetDestIgnoreMembers<TSource, TDest>(IConfig<TSource, TDest>? config)
    {
        return config?.Clauses.OfType<IIgnoreClause<TSource, TDest>>()
                     .Where(ic => ic is
                     {
                         IsValidClause: true,
                         DestinationIgnoreMember: not null
                     })
                     .Select(ic => ic.DestinationIgnoreMember!) ??
               [];
    }

    /// <summary>
    /// Gets the source ignore members from the configuration.
    /// 从配置中获取源忽略成员。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <param name="config">The configuration. 配置。</param>
    /// <returns>A collection of source ignore members. 源忽略成员的集合。</returns>
    public static IEnumerable<MappingMember> GetSourceIgnoreMembers<TSource, TDest>(IConfig<TSource, TDest>? config)
    {
        return config?.Clauses.OfType<IIgnoreClause<TSource, TDest>>()
                     .Where(ic => ic is
                     {
                         IsValidClause: true,
                         SourceIgnoreMember: not null
                     })
                     .Select(ic => ic.SourceIgnoreMember!) ??
               [];
    }

    /// <summary>
    /// Gets the map clauses from the configuration.
    /// 从配置中获取映射子句。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <param name="config">The configuration. 配置。</param>
    /// <returns>A collection of map clauses. 映射子句的集合。</returns>
    public static IEnumerable<IMapClause<TSource, TDest>> GetMapClauses<TSource, TDest>(IConfig<TSource, TDest>? config)
    {
        return config?.Clauses.OfType<IMapClause<TSource, TDest>>()
                     .Where(mc => mc.IsValidClause) ??
               [];
    }

    /// <summary>
    /// Gets the constructor clause from the configuration.
    /// 从配置中获取构造函数子句。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <param name="config">The configuration. 配置。</param>
    /// <returns>The constructor clause if found; otherwise, null. 如果找到则返回构造函数子句；否则返回 null。</returns>
    public static IConstructClause<TSource, TDest>? GetConstructorClause<TSource, TDest>(IConfig<TSource, TDest>? config)
    {
        return config?.Clauses.OfType<IConstructClause<TSource, TDest>>()
                     .FirstOrDefault(cc => cc.IsValidClause);
    }
}