using System.Collections.Concurrent;
using EasilyNET.ExpressMapper.MapBuilder.MapExpression;

namespace EasilyNET.ExpressMapper.Lambdas;

/// <summary>
/// Lambda 缓存接口。
/// Interface for Lambda cache.
/// </summary>
public interface ILambdaCache
{
    /// <summary>
    /// 获取或添加 Lambda。
    /// Gets or adds a Lambda.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="_faultFunc">故障函数。Fault function.</param>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    public IMapLambda<TSource, TDest> GetOrAdd<TSource, TDest>(Func<IMapLambda<TSource, TDest>> _faultFunc);
}

/// <summary>
/// Lambda 缓存实现类。
/// Implementation of the Lambda cache.
/// </summary>
public class LambdaCache : ILambdaCache
{
    private readonly ConcurrentDictionary<MapKey, IMapLambda> _lambdas = new();

    /// <summary>
    /// 获取或添加 Lambda。
    /// Gets or adds a Lambda.
    /// </summary>
    /// <typeparam name="TSource">源类型。Source type.</typeparam>
    /// <typeparam name="TDest">目标类型。Destination type.</typeparam>
    /// <param name="faultFunc">故障函数。Fault function.</param>
    /// <returns>映射 Lambda。Mapping Lambda.</returns>
    public IMapLambda<TSource, TDest> GetOrAdd<TSource, TDest>(Func<IMapLambda<TSource, TDest>> faultFunc)
    {
        var key = MapKey.Form<TSource, TDest>();
        return (IMapLambda<TSource, TDest>)_lambdas.GetOrAdd(key, _ => faultFunc());
    }
}