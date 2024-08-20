using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Expressions;
using EasilyNET.ExpressMapper.MapBuilder.MapExpression;

namespace EasilyNET.ExpressMapper.Lambdas;

/// <summary>
/// Lambda 构建器接口。
/// Interface for Lambda builder.
/// </summary>
public interface ILambdaBuilder
{
    /// <summary>
    /// 构建 Lambda 表达式。
    /// Builds a Lambda expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="configManager">配置管理器。Configuration manager.</param>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    MapLambda<TSource, TDest> BuildLambda<TSource, TDest>(IConfigManager configManager);
}

/// <summary>
/// Lambda 构建器实现类。
/// Implementation of the Lambda builder.
/// </summary>
/// <param name="expressionBuilder">表达式构建器。Expression builder.</param>
public class LambdaBuilder(IMapExpressionBuilder expressionBuilder) : ILambdaBuilder
{
    /// <summary>
    /// 构建 Lambda 表达式。
    /// Builds a Lambda expression.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="configManager">配置管理器。Configuration manager.</param>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    public MapLambda<TSource, TDest> BuildLambda<TSource, TDest>(IConfigManager configManager) => new() { Lambda = BuildLambdaCore<TSource, TDest>(configManager) };

    /// <summary>
    /// 构建 Lambda 核心方法。
    /// Builds the core Lambda method.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="configManager">配置管理器。Configuration manager.</param>
    /// <returns>Lambda 函数。Lambda function.</returns>
    private Func<TSource, TDest> BuildLambdaCore<TSource, TDest>(IConfigManager configManager) => expressionBuilder.FormExpression<TSource, TDest>(configManager).Compile();
}