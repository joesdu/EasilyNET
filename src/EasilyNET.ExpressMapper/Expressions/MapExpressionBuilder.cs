using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Expressions;

/// <summary>
/// 映射表达式构建器接口。
/// Interface for map expression builder.
/// </summary>
public interface IMapExpressionBuilder
{
    /// <summary>
    /// 生成映射表达式。
    /// Forms a mapping expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="configManager">配置管理器。Configuration manager.</param>
    /// <returns>映射表达式。Mapping expression.</returns>
    Expression<Func<TSource, TDest>> FormExpression<TSource, TDest>(IConfigManager configManager);
}

/// <summary>
/// 映射表达式构建器类，用于生成源类型和目标类型之间的映射表达式。
/// Class for map expression builder, used to form mapping expressions between source and destination types.
/// </summary>
public class MapExpressionBuilder(IExpressionBuilder expressionBuilder) : IMapExpressionBuilder
{
    /// <summary>
    /// 生成映射表达式。
    /// Forms a mapping expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="configManager">配置管理器。Configuration manager.</param>
    /// <returns>映射表达式。Mapping expression.</returns>
    public Expression<Func<TSource, TDest>> FormExpression<TSource, TDest>(IConfigManager configManager)
    {
        var config = configManager.GetConfig<TSource, TDest>();
        var mappingTracker = LibraryFactory.Instance.CreateMappingTracker(config)
                                           .RemoveIgnored()
                                           .MapConstructorParams()
                                           .AutoMapByName()
                                           .MapByClauses();
        return expressionBuilder.BuildExpression<TSource, TDest>(mappingTracker.GetMappingRules());
    }
}