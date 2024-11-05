namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// Interface for configuration builder with source and destination types.
/// 具有源类型和目标类型的配置构建器接口。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public interface IConfigurationBuilder<TSource, TDest> : IConfigBuilder, IMappingConfigure<TSource, TDest>;

/// <summary>
/// Interface for configuration builder.
/// 配置构建器接口。
/// </summary>
public interface IConfigBuilder
{
    /// <summary>
    /// Gets the configuration.
    /// 获取配置。
    /// </summary>
    public IConfig Config { get; }

    /// <summary>
    /// Gets the reverse configuration if available.
    /// 获取反向配置（如果有）。
    /// </summary>
    public IConfig? ReverseConfig { get; }
}