using System.Linq.Expressions;
using EasilyNET.ExpressMapper.Abstractions;
using EasilyNET.ExpressMapper.Abstractions.Clause;
using EasilyNET.ExpressMapper.Configuration.Config.Clause;

namespace EasilyNET.ExpressMapper.Configuration;

/// <summary>
/// 配置构建器类，用于构建源类型和目标类型之间的映射配置。
/// Class for configuration builder, used to build mapping configurations between source and destination types.
/// </summary>
/// <typeparam name="TSource">源类型。Source type.</typeparam>
/// <typeparam name="TDest">目标类型。Destination type.</typeparam>
public class ConfigBuilder<TSource, TDest> : IConfigurationBuilder<TSource, TDest>
{
    /// <summary>
    /// Collection of clauses.
    /// 子句集合。
    /// </summary>
    private readonly ICollection<IClause<TSource, TDest>> _clauses = [];

    /// <summary>
    /// Configuration instance.
    /// 配置实例。
    /// </summary>
    private IConfig<TSource, TDest>? _config;

    /// <summary>
    /// Reverse configuration instance.
    /// 反向配置实例。
    /// </summary>
    private IConfig<TDest, TSource>? _reverseConfig;

    private bool isTwoWaysConfig;

    /// <summary>
    /// Gets the configuration.
    /// 获取配置。
    /// </summary>
    public IConfig Config => _config ??= BuildConfig();

    /// <summary>
    /// Gets the reverse configuration if available.
    /// 获取反向配置（如果有）。
    /// </summary>
    public IConfig? ReverseConfig
    {
        get
        {
            if (!isTwoWaysConfig) return null;
            _reverseConfig ??= BuildReverseConfig();
            return _reverseConfig;
        }
    }

    /// <summary>
    /// 定义源类型成员到目标类型成员的映射。
    /// Defines the mapping from a source type member to a destination type member.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="destMember">目标成员表达式。Destination member expression.</param>
    /// <param name="lambda">源成员表达式。Source member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> Map<TMember>(Expression<Func<TDest, TMember>> destMember, Expression<Func<TSource, TMember>> lambda)
    {
        _clauses.Add(new MapClause<TSource, TDest, TMember>(destMember, lambda));
        return this;
    }

    /// <summary>
    /// 忽略目标类型中的某个成员。
    /// Ignores a member in the destination type.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="destMember">目标成员表达式。Destination member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> IgnoreDest<TMember>(Expression<Func<TDest, TMember>> destMember)
    {
        _clauses.Add(new IgnoreClause<TSource, TDest, TMember>(destMember));
        return this;
    }

    /// <summary>
    /// 忽略源类型中的某个成员。
    /// Ignores a member in the source type.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="sourceMember">源成员表达式。Source member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> IgnoreSource<TMember>(Expression<Func<TSource, TMember>> sourceMember)
    {
        _clauses.Add(new IgnoreClause<TSource, TDest, TMember>(sourceMember));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有一个参数。
    /// Specifies the constructor of the destination type with one parameter.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有两个参数。
    /// Specifies the constructor of the destination type with two parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有三个参数。
    /// Specifies the constructor of the destination type with three parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2), typeof(T3)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有四个参数。
    /// Specifies the constructor of the destination type with four parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <typeparam name="T4">第四个参数的类型。Type of the fourth parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2), typeof(T3), typeof(T4)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有五个参数。
    /// Specifies the constructor of the destination type with five parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <typeparam name="T4">第四个参数的类型。Type of the fourth parameter.</typeparam>
    /// <typeparam name="T5">第五个参数的类型。Type of the fifth parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有六个参数。
    /// Specifies the constructor of the destination type with six parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <typeparam name="T4">第四个参数的类型。Type of the fourth parameter.</typeparam>
    /// <typeparam name="T5">第五个参数的类型。Type of the fifth parameter.</typeparam>
    /// <typeparam name="T6">第六个参数的类型。Type of the sixth parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5, T6>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有七个参数。
    /// Specifies the constructor of the destination type with seven parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <typeparam name="T4">第四个参数的类型。Type of the fourth parameter.</typeparam>
    /// <typeparam name="T5">第五个参数的类型。Type of the fifth parameter.</typeparam>
    /// <typeparam name="T6">第六个参数的类型。Type of the sixth parameter.</typeparam>
    /// <typeparam name="T7">第七个参数的类型。Type of the seventh parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    public IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5, T6, T7>()
    {
        var info = typeof(TDest).GetConstructor([typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6), typeof(T7)]);
        _clauses.Add(new ConstructorClause<TSource, TDest>(info));
        return this;
    }

    /// <summary>
    /// 启用双向构建映射配置。
    /// Enables two-way mapping configuration.
    /// </summary>
    public void TwoWaysBuilding()
    {
        isTwoWaysConfig = true;
    }

    /// <summary>
    /// Builds the configuration.
    /// 构建配置。
    /// </summary>
    /// <returns>The configuration. 配置。</returns>
    private IConfig<TSource, TDest> BuildConfig() => LibraryFactory.Instance.CreateConfig(_clauses);

    /// <summary>
    /// Builds the reverse configuration.
    /// 构建反向配置。
    /// </summary>
    /// <returns>The reverse configuration. 反向配置。</returns>
    private IConfig<TDest, TSource> BuildReverseConfig()
    {
        return LibraryFactory.Instance.CreateConfig(_clauses.OfType<IReverseAbleClause<TSource, TDest>>()
                                                            .Select(c => c.GetReverseClause()).ToList());
    }
}