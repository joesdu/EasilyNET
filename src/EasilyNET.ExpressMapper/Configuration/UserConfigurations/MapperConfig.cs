using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Configuration;

/// <summary>
/// Abstract class for mapper configuration.
/// 映射器配置的抽象类。
/// </summary>
/// <typeparam name="TSource">The source type. 源类型。</typeparam>
/// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
public abstract class MapperConfig<TSource, TDest> : IConfigProvider
{
    /// <summary>
    /// Configuration builder instance.
    /// 配置构建器实例。
    /// </summary>
    private readonly IConfigurationBuilder<TSource, TDest> _builder = LibraryFactory.Instance.CreateConfigBuilder<TSource, TDest>();

    /// <summary>
    /// Gets the configuration units.
    /// 获取配置单元。
    /// </summary>
    /// <returns>Enumeration of configuration units. 配置单元的枚举。</returns>
    IEnumerable<IConfig> IConfigProvider.GetConfigUnits()
    {
        Configure(_builder);
        yield return _builder.Config;
        if (_builder.ReverseConfig is not null)
            yield return _builder.ReverseConfig;
    }

    /// <summary>
    /// Configures the mappings. Must be implemented by derived classes.
    /// 配置映射。必须由派生类实现。
    /// </summary>
    /// <param name="configure">The mapping configure. 映射配置器。</param>
    abstract protected void Configure(IMappingConfigure<TSource, TDest> configure);
}