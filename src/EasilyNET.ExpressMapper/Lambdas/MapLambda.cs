namespace EasilyNET.ExpressMapper.MapBuilder.MapExpression;

/// <summary>
/// 映射 Lambda 接口。
/// Interface for mapping Lambda.
/// </summary>
public interface IMapLambda : IKeyKeeper;

/// <summary>
/// 泛型映射 Lambda 接口。
/// Generic interface for mapping Lambda.
/// </summary>
/// <typeparam name="TSource">源类型。Source type.</typeparam>
/// <typeparam name="TDest">目标类型。Destination type.</typeparam>
public interface IMapLambda<in TSource, out TDest> : IMapLambda
{
    /// <summary>
    /// 获取 Lambda 函数。
    /// Gets the Lambda function.
    /// </summary>
    public Func<TSource, TDest> Lambda { get; }
}

/// <summary>
/// 映射 Lambda 实现类。
/// Implementation of the mapping Lambda.
/// </summary>
/// <typeparam name="TSource">源类型。Source type.</typeparam>
/// <typeparam name="TDest">目标类型。Destination type.</typeparam>
public class MapLambda<TSource, TDest> : IMapLambda<TSource, TDest>
{
    /// <summary>
    /// 获取或设置 Lambda 函数。
    /// Gets or sets the Lambda function.
    /// </summary>
    public required Func<TSource, TDest> Lambda { get; init; }

    /// <summary>
    /// 获取映射键。
    /// Gets the mapping key.
    /// </summary>
    public MapKey Key => MapKey.Form<TSource, TDest>();
}