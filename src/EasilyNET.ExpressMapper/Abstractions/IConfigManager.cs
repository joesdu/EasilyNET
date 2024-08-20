namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// Interface for configuration manager.
/// 配置管理器接口。
/// </summary>
public interface IConfigManager
{
    /// <summary>
    /// Gets the configuration for the specified source and destination types.
    /// 获取指定源类型和目标类型的配置。
    /// </summary>
    /// <typeparam name="TSource">The source type. 源类型。</typeparam>
    /// <typeparam name="TDest">The destination type. 目标类型。</typeparam>
    /// <returns>The configuration if found; otherwise, null. 如果找到配置，则返回配置；否则返回 null。</returns>
    IConfig<TSource, TDest>? GetConfig<TSource, TDest>();
}