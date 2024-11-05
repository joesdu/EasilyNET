using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Configuration;
using EasilyNET.ExpressMapper.Expressions;
using EasilyNET.ExpressMapper.Lambdas;
using EasilyNET.ExpressMapper.MapBuilder;

// ReSharper disable UnusedMemberInSuper.Global
namespace EasilyNET.ExpressMapper;

/// <summary>
/// The LibraryFactory class implements the ILibraryFactory interface and provides methods to create various mapping-related objects.
/// LibraryFactory 类实现了 ILibraryFactory 接口，提供了创建各种映射相关对象的方法。
/// </summary>
public sealed class LibraryFactory : ILibraryFactory
{
    static LibraryFactory()
    {
        Instance = new LibraryFactory();
    }

    /// <summary>
    /// Gets the singleton instance of the LibraryFactory.
    /// 获取 LibraryFactory 的单例实例。
    /// </summary>
    public static ILibraryFactory Instance { get; }

    /// <summary>
    /// Creates a new instance of LambdaCache.
    /// 创建一个新的 LambdaCache 实例。
    /// </summary>
    public ILambdaCache CreateLambdaCache() => new LambdaCache();

    /// <summary>
    /// Creates a new instance of Config.
    /// 创建一个新的 Config 实例。
    /// </summary>
    public IConfig<TSource, TDest> CreateConfig<TSource, TDest>(IEnumerable<IClause<TSource, TDest>> clauses) => new Config<TSource, TDest>(clauses);

    /// <summary>
    /// Creates a new instance of ExpressionBuilder.
    /// 创建一个新的 ExpressionBuilder 实例。
    /// </summary>
    public IExpressionBuilder CreateExpressionBuilder() => new ExpressionBuilder();

    /// <summary>
    /// Creates a new instance of ConfigBuilder.
    /// 创建一个新的 ConfigBuilder 实例。
    /// </summary>
    public IConfigurationBuilder<TSource, TDest> CreateConfigBuilder<TSource, TDest>() => new ConfigBuilder<TSource, TDest>();

    /// <summary>
    /// Creates a new instance of ConfigManager.
    /// 创建一个新的 ConfigManager 实例。
    /// </summary>
    public IConfigManager CreateConfigManager(IEnumerable<IConfigProvider> providers) => ConfigManager.CreateManager(providers);

    /// <summary>
    /// Creates a new instance of LambdaManager.
    /// 创建一个新的 LambdaManager 实例。
    /// </summary>
    public ILambdaManager CreateLambdaManager(IConfigManager configManager) =>
        new LambdaManager(CreateLambdaCache(),
            CreateLambdaBuilder(),
            configManager);

    /// <summary>
    /// Creates a new instance of LambdaBuilder.
    /// 创建一个新的 LambdaBuilder 实例。
    /// </summary>
    public ILambdaBuilder CreateLambdaBuilder() => new LambdaBuilder(CreateMapExpressionBuilder());

    /// <summary>
    /// Creates a new instance of MapExpressionBuilder.
    /// 创建一个新的 MapExpressionBuilder 实例。
    /// </summary>
    public IMapExpressionBuilder CreateMapExpressionBuilder() => new MapExpressionBuilder(CreateExpressionBuilder());

    /// <summary>
    /// Creates a new instance of MappingTracker.
    /// 创建一个新的 MappingTracker 实例。
    /// </summary>
    public IMappingTracker<TSource, TDest> CreateMappingTracker<TSource, TDest>(IConfig<TSource, TDest>? config) => new MappingTracker<TSource, TDest>(config);
}