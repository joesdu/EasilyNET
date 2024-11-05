using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.MapBuilder;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.ExpressMapper.Mapper;

/// <summary>
/// Mapper 类
/// </summary>
public class Mapper : IMapper
{
    private readonly ILambdaManager _lambdaManager;

    /// <summary>
    /// 构造函数，初始化lambda管理器
    /// Constructor to initialize the lambda manager
    /// </summary>
    /// <param name="lambdaManager"></param>
    protected Mapper(ILambdaManager lambdaManager)
    {
        _lambdaManager = lambdaManager;
    }

    /// <summary>
    /// 将源对象映射到目标类型。
    /// Maps the source object to the destination type.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TDest"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public TDest Map<TSource, TDest>(TSource source)
    {
        var func = _lambdaManager.GetLambda<TSource, TDest>();
        return func(source);
    }

    /// <summary>
    /// 创建一个新的 IMapper 实例
    /// Creates a new IMapper instance.
    /// </summary>
    /// <returns></returns>
    public static IMapper Create() => new Mapper(LibraryFactory.Instance.CreateLambdaManager(LibraryFactory.Instance.CreateConfigManager([])));

    /// <summary>
    /// 使用单个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with a single configuration provider.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <returns></returns>
    public static IMapper<T1> Create<T1>()
        where T1 : IConfigProvider, new() =>
        new Mapper<T1>(LibraryFactory.Instance.CreateConfigManager([new T1()]));

    /// <summary>
    /// 使用指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with a specified configuration provider.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <param name="provider1"></param>
    /// <returns></returns>
    public static IMapper<T1> Create<T1>(T1 provider1)
        where T1 : IConfigProvider =>
        new Mapper<T1>(LibraryFactory.Instance.CreateConfigManager([provider1]));

    /// <summary>
    /// 使用两个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with two configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2> Create<T1, T2>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new() =>
        new Mapper<T1, T2>(LibraryFactory.Instance.CreateConfigManager([new T1(), new T2()]));

    /// <summary>
    /// 使用两个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with two specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <returns></returns>
    public static IMapper<T1, T2> Create<T1, T2>(T1 provider1, T2 provider2)
        where T1 : IConfigProvider
        where T2 : IConfigProvider =>
        new Mapper<T1, T2>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2]));

    /// <summary>
    /// 使用三个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with three configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3> Create<T1, T2, T3>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3>(LibraryFactory.Instance.CreateConfigManager([new T1(), new T2(), new T3()]));

    /// <summary>
    /// 使用三个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with three specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3> Create<T1, T2, T3>(T1 provider1, T2 provider2, T3 provider3)
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider =>
        new Mapper<T1, T2, T3>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2, provider3]));

    /// <summary>
    /// 使用四个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with four configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4> Create<T1, T2, T3, T4>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4>(LibraryFactory.Instance.CreateConfigManager([new T1(), new T2(), new T3(), new T4()]));

    /// <summary>
    /// 使用四个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with four specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4> Create<T1, T2, T3, T4>(T1 provider1, T2 provider2, T3 provider3, T4 provider4)
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2, provider3, provider4]));

    /// <summary>
    /// 使用五个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with five configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5>(LibraryFactory.Instance
                                                     .CreateConfigManager([new T1(), new T2(), new T3(), new T4(), new T5()]));

    /// <summary>
    /// 使用五个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with five specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5> Create<T1, T2, T3, T4, T5>(T1 provider1, T2 provider2, T3 provider3, T4 provider4, T5 provider5)
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2, provider3, provider4, provider5]));

    /// <summary>
    /// 使用六个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with six configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new()
        where T6 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5, T6>(LibraryFactory.Instance.CreateConfigManager([new T1(), new T2(), new T3(), new T4(), new T5(), new T6()]));

    /// <summary>
    /// 使用六个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with six specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <param name="provider6"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6> Create<T1, T2, T3, T4, T5, T6>(T1 provider1, T2 provider2, T3 provider3, T4 provider4, T5 provider5, T6 provider6)
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider
        where T6 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5, T6>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2, provider3, provider4, provider5, provider6]));

    /// <summary>
    /// 使用七个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with seven configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new()
        where T6 : IConfigProvider, new()
        where T7 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7>(LibraryFactory.Instance.CreateConfigManager([new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7()]));

    /// <summary>
    /// 使用七个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with seven specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <param name="provider6"></param>
    /// <param name="provider7"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7> Create<T1, T2, T3, T4, T5, T6, T7>(T1 provider1, T2 provider2, T3 provider3, T4 provider4, T5 provider5, T6 provider6, T7 provider7)
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider
        where T6 : IConfigProvider
        where T7 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7>(LibraryFactory.Instance
                                                             .CreateConfigManager([provider1, provider2, provider3, provider4, provider5, provider6, provider7]));

    /// <summary>
    /// 使用八个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with eight configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new()
        where T6 : IConfigProvider, new()
        where T7 : IConfigProvider, new()
        where T8 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8>(LibraryFactory.Instance
                                                                 .CreateConfigManager([new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8()]));

    /// <summary>
    /// 使用八个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with eight specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <param name="provider6"></param>
    /// <param name="provider7"></param>
    /// <param name="provider8"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8> Create<T1, T2, T3, T4, T5, T6, T7, T8>(
        T1 provider1,
        T2 provider2,
        T3 provider3,
        T4 provider4,
        T5 provider5,
        T6 provider6,
        T7 provider7,
        T8 provider8
    )
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider
        where T6 : IConfigProvider
        where T7 : IConfigProvider
        where T8 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8>(LibraryFactory.Instance.CreateConfigManager([provider1, provider2, provider3, provider4, provider5, provider6, provider7, provider8]));

    /// <summary>
    /// 使用九个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with nine configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new()
        where T6 : IConfigProvider, new()
        where T7 : IConfigProvider, new()
        where T8 : IConfigProvider, new()
        where T9 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9>(LibraryFactory.Instance
                                                                     .CreateConfigManager([new T1(), new T2(), new T3(), new T4(), new T5(), new T6(), new T7(), new T8(), new T9()]));

    /// <summary>
    /// 使用九个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with nine specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <param name="provider6"></param>
    /// <param name="provider7"></param>
    /// <param name="provider8"></param>
    /// <param name="provider9"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9> Create<T1, T2, T3, T4, T5, T6, T7, T8, T9>(
        T1 provider1,
        T2 provider2,
        T3 provider3,
        T4 provider4,
        T5 provider5,
        T6 provider6,
        T7 provider7,
        T8 provider8,
        T9 provider9
    )
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider
        where T6 : IConfigProvider
        where T7 : IConfigProvider
        where T8 : IConfigProvider
        where T9 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9>(LibraryFactory.Instance
                                                                     .CreateConfigManager([
                                                                         provider1, provider2, provider3, provider4, provider5,
                                                                         provider6, provider7, provider8, provider9
                                                                     ]));

    /// <summary>
    /// 使用十个配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with ten configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <typeparam name="T10"></typeparam>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>()
        where T1 : IConfigProvider, new()
        where T2 : IConfigProvider, new()
        where T3 : IConfigProvider, new()
        where T4 : IConfigProvider, new()
        where T5 : IConfigProvider, new()
        where T6 : IConfigProvider, new()
        where T7 : IConfigProvider, new()
        where T8 : IConfigProvider, new()
        where T9 : IConfigProvider, new()
        where T10 : IConfigProvider, new() =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LibraryFactory.Instance.CreateConfigManager([
            new T1(), new T2(), new T3(), new T4(), new T5(),
            new T6(), new T7(), new T8(), new T9(), new T10()
        ]));

    /// <summary>
    /// 使用十个指定的配置提供程序创建一个新的 IMapper 实例
    /// Creates a new IMapper instance with ten specified configuration providers.
    /// </summary>
    /// <typeparam name="T1"></typeparam>
    /// <typeparam name="T2"></typeparam>
    /// <typeparam name="T3"></typeparam>
    /// <typeparam name="T4"></typeparam>
    /// <typeparam name="T5"></typeparam>
    /// <typeparam name="T6"></typeparam>
    /// <typeparam name="T7"></typeparam>
    /// <typeparam name="T8"></typeparam>
    /// <typeparam name="T9"></typeparam>
    /// <typeparam name="T10"></typeparam>
    /// <param name="provider1"></param>
    /// <param name="provider2"></param>
    /// <param name="provider3"></param>
    /// <param name="provider4"></param>
    /// <param name="provider5"></param>
    /// <param name="provider6"></param>
    /// <param name="provider7"></param>
    /// <param name="provider8"></param>
    /// <param name="provider9"></param>
    /// <param name="provider10"></param>
    /// <returns></returns>
    public static IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
        Create<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
            T1 provider1,
            T2 provider2,
            T3 provider3,
            T4 provider4,
            T5 provider5,
            T6 provider6,
            T7 provider7,
            T8 provider8,
            T9 provider9,
            T10 provider10
        )
        where T1 : IConfigProvider
        where T2 : IConfigProvider
        where T3 : IConfigProvider
        where T4 : IConfigProvider
        where T5 : IConfigProvider
        where T6 : IConfigProvider
        where T7 : IConfigProvider
        where T8 : IConfigProvider
        where T9 : IConfigProvider
        where T10 : IConfigProvider =>
        new Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(LibraryFactory.Instance.CreateConfigManager([
            provider1, provider2, provider3, provider4, provider5,
            provider6, provider7, provider8, provider9, provider10
        ]));
}

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T>
    where T : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2>
    where T1 : IConfigProvider
    where T2 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5, T6>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5, T6>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5, T6, T7>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5, T6, T7>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5, T6, T7, T8>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5, T6, T7, T8>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider
    where T8 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider
    where T8 : IConfigProvider
    where T9 : IConfigProvider;

/// <summary>
/// 初始化 <see cref="Mapper{T}" /> 类的新实例
/// Initializes a new instance of the <see cref="Mapper{T}" /> class.
/// </summary>
/// <param name="configManager">配置管理器 The configuration manager.</param>
public class Mapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IConfigManager configManager) : Mapper(LibraryFactory.Instance.CreateLambdaManager(configManager)), IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider
    where T8 : IConfigProvider
    where T9 : IConfigProvider
    where T10 : IConfigProvider;