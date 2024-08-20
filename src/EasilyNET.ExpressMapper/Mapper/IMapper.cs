using EasilyNET.ExpressMapper.Abstractions;

// ReSharper disable UnusedTypeParameter

namespace EasilyNET.ExpressMapper.Mapper;

/// <summary>
/// 合同用于将对象从一种类型映射到另一种类型。
/// Contract for mapping objects from one type to another.
/// </summary>
public interface IMapper
{
    /// <summary>
    /// 将类型 <typeparamref name="TSource" /> 的对象映射到类型 <typeparamref name="TDest" /> 的对象。
    /// Maps an object of type <typeparamref name="TSource" /> to an object of type <typeparamref name="TDest" />.
    /// </summary>
    /// <typeparam name="TSource">要映射的源类型。The source type to map from.</typeparam>
    /// <typeparam name="TDest">要映射到的目标类型。The destination type to map to.</typeparam>
    /// <returns>
    /// 从类型 <typeparamref name="TSource" /> 的对象映射到类型 <typeparamref name="TDest" /> 的对象。
    /// An object of type <typeparamref name="TDest" /> mapped from an object of type <typeparamref name="TSource" />.
    /// </returns>
    TDest Map<TSource, TDest>(TSource source);
}

/// <summary>
/// 泛型映射接口，带有一个配置提供程序。
/// Generic mapping interface with one configuration provider.
/// </summary>
/// <typeparam name="T1">配置提供程序类型。Type of the configuration provider.</typeparam>
public interface IMapper<T1> : IMapper
    where T1 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有两个配置提供程序。
/// Generic mapping interface with two configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
public interface IMapper<T1, T2> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有三个配置提供程序。
/// Generic mapping interface with three configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
public interface IMapper<T1, T2, T3> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有四个配置提供程序。
/// Generic mapping interface with four configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有五个配置提供程序。
/// Generic mapping interface with five configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有六个配置提供程序。
/// Generic mapping interface with six configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
/// <typeparam name="T6">第六个配置提供程序类型。Type of the sixth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5, T6> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有七个配置提供程序。
/// Generic mapping interface with seven configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
/// <typeparam name="T6">第六个配置提供程序类型。Type of the sixth configuration provider.</typeparam>
/// <typeparam name="T7">第七个配置提供程序类型。Type of the seventh configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5, T6, T7> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有八个配置提供程序。
/// Generic mapping interface with eight configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
/// <typeparam name="T6">第六个配置提供程序类型。Type of the sixth configuration provider.</typeparam>
/// <typeparam name="T7">第七个配置提供程序类型。Type of the seventh configuration provider.</typeparam>
/// <typeparam name="T8">第八个配置提供程序类型。Type of the eighth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5, T6, T7, T8> : IMapper
    where T1 : IConfigProvider
    where T2 : IConfigProvider
    where T3 : IConfigProvider
    where T4 : IConfigProvider
    where T5 : IConfigProvider
    where T6 : IConfigProvider
    where T7 : IConfigProvider
    where T8 : IConfigProvider;

/// <summary>
/// 泛型映射接口，带有九个配置提供程序。
/// Generic mapping interface with nine configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
/// <typeparam name="T6">第六个配置提供程序类型。Type of the sixth configuration provider.</typeparam>
/// <typeparam name="T7">第七个配置提供程序类型。Type of the seventh configuration provider.</typeparam>
/// <typeparam name="T8">第八个配置提供程序类型。Type of the eighth configuration provider.</typeparam>
/// <typeparam name="T9">第九个配置提供程序类型。Type of the ninth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9> : IMapper
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
/// 泛型映射接口，带有十个配置提供程序。
/// Generic mapping interface with ten configuration providers.
/// </summary>
/// <typeparam name="T1">第一个配置提供程序类型。Type of the first configuration provider.</typeparam>
/// <typeparam name="T2">第二个配置提供程序类型。Type of the second configuration provider.</typeparam>
/// <typeparam name="T3">第三个配置提供程序类型。Type of the third configuration provider.</typeparam>
/// <typeparam name="T4">第四个配置提供程序类型。Type of the fourth configuration provider.</typeparam>
/// <typeparam name="T5">第五个配置提供程序类型。Type of the fifth configuration provider.</typeparam>
/// <typeparam name="T6">第六个配置提供程序类型。Type of the sixth configuration provider.</typeparam>
/// <typeparam name="T7">第七个配置提供程序类型。Type of the seventh configuration provider.</typeparam>
/// <typeparam name="T8">第八个配置提供程序类型。Type of the eighth configuration provider.</typeparam>
/// <typeparam name="T9">第九个配置提供程序类型。Type of the ninth configuration provider.</typeparam>
/// <typeparam name="T10">第十个配置提供程序类型。Type of the tenth configuration provider.</typeparam>
public interface IMapper<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : IMapper
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