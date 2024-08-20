using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Configuration.UserConfigurations;

/// <summary>
/// Abstract class for composite configuration.
/// 复合配置的抽象类。
/// </summary>
public abstract class CompositeConfig : IConfigProvider
{
    /// <summary>
    /// Collection of configuration builders.
    /// 配置构建器集合。
    /// </summary>
    private readonly ICollection<IConfigBuilder> _configBuilders = [];

    /// <summary>
    /// Gets the configuration units.
    /// 获取配置单元。
    /// </summary>
    /// <returns>Enumeration of configuration units. 配置单元的枚举。</returns>
    IEnumerable<IConfig> IConfigProvider.GetConfigUnits()
    {
        Configure();
        foreach (var configBuilder in _configBuilders)
        {
            yield return configBuilder.Config;
            if (configBuilder.ReverseConfig is not null)
                yield return configBuilder.ReverseConfig;
        }
    }

    /// <summary>
    /// Configures the mappings. Must be implemented by derived classes.
    /// 配置映射。必须由派生类实现。
    /// </summary>
    abstract protected void Configure();

    /// <summary>
    /// Creates a new configuration for the specified source and destination types.
    /// 为指定的源类型和目标类型创建新配置。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <returns>The mapping configuration. 映射配置。</returns>
    protected IMappingConfigure<TSource, TDest> NewConfiguration<TSource, TDest>()
    {
        var builder = LibraryFactory.Instance.CreateConfigBuilder<TSource, TDest>();
        _configBuilders.Add(builder);
        return builder;
    }
}