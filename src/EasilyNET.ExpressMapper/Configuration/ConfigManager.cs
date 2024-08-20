using System.Collections.Immutable;
using EasilyNET.ExpressMapper.Abstractions;

namespace EasilyNET.ExpressMapper.Configuration;

/// <summary>
/// Implementation of the configuration manager.
/// 配置管理器的实现。
/// </summary>
public class ConfigManager : IConfigManager
{
    /// <summary>
    /// Dictionary of configurations.
    /// 配置字典。
    /// </summary>
    private readonly ImmutableDictionary<MapKey, IConfig> _configs;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigManager" /> class.
    /// 初始化 <see cref="ConfigManager" /> 类的新实例。
    /// </summary>
    /// <param name="configs">The configurations. 配置。</param>
    private ConfigManager(ImmutableDictionary<MapKey, IConfig> configs)
    {
        _configs = configs;
    }

    /// <summary>
    /// Gets the configuration for the specified source and destination types.
    /// 获取指定源类型和目标类型的配置。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <returns>The configuration if found; otherwise, null. 如果找到配置，则返回配置；否则返回 null。</returns>
    public IConfig<TSource, TDest>? GetConfig<TSource, TDest>()
    {
        _configs.TryGetValue(MapKey.Form<TSource, TDest>(), out var config);
        return config as IConfig<TSource, TDest>;
    }

    /// <summary>
    /// Creates a new configuration manager with the specified providers.
    /// 使用指定的提供程序创建新的配置管理器。
    /// </summary>
    /// <param name="providers">The configuration providers. 配置提供程序。</param>
    /// <returns>The configuration manager. 配置管理器。</returns>
    public static IConfigManager CreateManager(IEnumerable<IConfigProvider> providers)
    {
        var pairs = providers
                    .SelectMany(provider => provider.GetConfigUnits())
                    .Select(config => new KeyValuePair<MapKey, IConfig>(config.Key, config));
        return new ConfigManager(ImmutableDictionary.CreateRange(pairs));
    }
}