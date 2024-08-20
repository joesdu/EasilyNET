using System.Linq.Expressions;

namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// 映射配置接口，用于定义源类型和目标类型之间的映射规则。
/// Interface for mapping configuration, used to define mapping rules between source and destination types.
/// </summary>
/// <typeparam name="TSource">源类型。Source type.</typeparam>
/// <typeparam name="TDest">目标类型。Destination type.</typeparam>
public interface IMappingConfigure<TSource, TDest>
{
    /// <summary>
    /// 定义源类型成员到目标类型成员的映射。
    /// Defines the mapping from a source type member to a destination type member.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="destMember">目标成员表达式。Destination member expression.</param>
    /// <param name="lambda">源成员表达式。Source member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> Map<TMember>(Expression<Func<TDest, TMember>> destMember, Expression<Func<TSource, TMember>> lambda);

    /// <summary>
    /// 忽略目标类型中的某个成员。
    /// Ignores a member in the destination type.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="destMember">目标成员表达式。Destination member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> IgnoreDest<TMember>(Expression<Func<TDest, TMember>> destMember);

    /// <summary>
    /// 忽略源类型中的某个成员。
    /// Ignores a member in the source type.
    /// </summary>
    /// <typeparam name="TMember">成员类型。Member type.</typeparam>
    /// <param name="sourceMember">源成员表达式。Source member expression.</param>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> IgnoreSource<TMember>(Expression<Func<TSource, TMember>> sourceMember);

    /// <summary>
    /// 启用双向构建映射配置。
    /// Enables two-way mapping configuration.
    /// </summary>
    void TwoWaysBuilding();

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有一个参数。
    /// Specifies the constructor of the destination type with one parameter.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> WithConstructor<T1>();

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有两个参数。
    /// Specifies the constructor of the destination type with two parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2>();

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有三个参数。
    /// Specifies the constructor of the destination type with three parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3>();

    /// <summary>
    /// 指定目标类型的构造函数，构造函数有四个参数。
    /// Specifies the constructor of the destination type with four parameters.
    /// </summary>
    /// <typeparam name="T1">第一个参数的类型。Type of the first parameter.</typeparam>
    /// <typeparam name="T2">第二个参数的类型。Type of the second parameter.</typeparam>
    /// <typeparam name="T3">第三个参数的类型。Type of the third parameter.</typeparam>
    /// <typeparam name="T4">第四个参数的类型。Type of the fourth parameter.</typeparam>
    /// <returns>映射配置接口。Mapping configuration interface.</returns>
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4>();

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
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5>();

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
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5, T6>();

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
    IMappingConfigure<TSource, TDest> WithConstructor<T1, T2, T3, T4, T5, T6, T7>();
}