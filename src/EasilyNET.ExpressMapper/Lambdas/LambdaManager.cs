using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Lambdas;
using EasilyNET.ExpressMapper.MapBuilder.MapExpression;

namespace EasilyNET.ExpressMapper.MapBuilder;

/// <summary>
/// Lambda 管理器接口。
/// Interface for Lambda manager.
/// </summary>
public interface ILambdaManager
{
    /// <summary>
    /// 获取 Lambda 函数。
    /// Gets the Lambda function.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <returns>Lambda 函数。Lambda function.</returns>
    Func<TSource, TDest> GetLambda<TSource, TDest>();
}

/// <summary>
/// Lambda 管理器实现类。
/// Implementation of the Lambda manager.
/// </summary>
/// <param name="cache">Lambda 缓存。Lambda cache.</param>
/// <param name="lambdaBuilder">Lambda 构建器。Lambda builder.</param>
/// <param name="configManager">配置管理器。Configuration manager.</param>
public class LambdaManager(ILambdaCache cache, ILambdaBuilder lambdaBuilder, IConfigManager configManager) : ILambdaManager
{
    /// <summary>
    /// 获取 Lambda 函数。
    /// Gets the Lambda function.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <returns>Lambda 函数。Lambda function.</returns>
    public Func<TSource, TDest> GetLambda<TSource, TDest>() => GetMapLambda<TSource, TDest>().Lambda;

    /// <summary>
    /// 获取映射 Lambda。
    /// Gets the mapping Lambda.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    private IMapLambda<TSource, TDest> GetMapLambda<TSource, TDest>() => cache.GetOrAdd(ConstructLambda<TSource, TDest>);

    /// <summary>
    /// 构建映射 Lambda。
    /// Constructs the mapping Lambda.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    private MapLambda<TSource, TDest> ConstructLambda<TSource, TDest>() => lambdaBuilder.BuildLambda<TSource, TDest>(configManager);
}