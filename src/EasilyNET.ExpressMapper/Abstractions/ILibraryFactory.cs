using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Expressions;
using EasilyNET.ExpressMapper.Lambdas;
using EasilyNET.ExpressMapper.MapBuilder;

namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// Defines a factory interface for creating various mapping-related objects.
/// 定义了一个工厂接口，用于创建各种映射相关的对象。
/// </summary>
public interface ILibraryFactory
{
    /// <summary>
    /// Creates a new instance of LambdaCache.
    /// 创建一个新的 LambdaCache 实例。
    /// </summary>
    ILambdaCache CreateLambdaCache();

    /// <summary>
    /// Creates a new instance of Config.
    /// 创建一个新的 Config 实例。
    /// </summary>
    IConfig<TSource, TDest> CreateConfig<TSource, TDest>(IEnumerable<IClause<TSource, TDest>> clauses);

    /// <summary>
    /// Creates a new instance of ExpressionBuilder.
    /// 创建一个新的 ExpressionBuilder 实例。
    /// </summary>
    IExpressionBuilder CreateExpressionBuilder();

    /// <summary>
    /// Creates a new instance of ConfigBuilder.
    /// 创建一个新的 ConfigBuilder 实例。
    /// </summary>
    IConfigurationBuilder<TSource, TDest> CreateConfigBuilder<TSource, TDest>();

    /// <summary>
    /// Creates a new instance of ConfigManager.
    /// 创建一个新的 ConfigManager 实例。
    /// </summary>
    IConfigManager CreateConfigManager(IEnumerable<IConfigProvider> providers);

    /// <summary>
    /// Creates a new instance of LambdaManager.
    /// 创建一个新的 LambdaManager 实例。
    /// </summary>
    ILambdaManager CreateLambdaManager(IConfigManager manager);

    /// <summary>
    /// Creates a new instance of LambdaBuilder.
    /// 创建一个新的 LambdaBuilder 实例。
    /// </summary>
    ILambdaBuilder CreateLambdaBuilder();

    /// <summary>
    /// Creates a new instance of MapExpressionBuilder.
    /// 创建一个新的 MapExpressionBuilder 实例。
    /// </summary>
    IMapExpressionBuilder CreateMapExpressionBuilder();

    /// <summary>
    /// Creates a new instance of MappingTracker.
    /// 创建一个新的 MappingTracker 实例。
    /// </summary>
    IMappingTracker<TSource, TDest> CreateMappingTracker<TSource, TDest>(IConfig<TSource, TDest>? config);
}