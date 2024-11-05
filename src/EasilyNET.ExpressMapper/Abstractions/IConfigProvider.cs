namespace EasilyNET.ExpressMapper.Abstractions;

/// <summary>
/// Interface for configuration provider.
/// 配置提供程序接口。
/// </summary>
public interface IConfigProvider
{
    /// <summary>
    /// Gets the configuration units.
    /// 获取配置单元。
    /// </summary>
    /// <returns>Enumeration of configuration units. 配置单元的枚举。</returns>
    public IEnumerable<IConfig> GetConfigUnits();
}