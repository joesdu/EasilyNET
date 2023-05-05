// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.RabbitBus.Configs;

/// <summary>
/// Amqp端点
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class BaseConfig
{
    /// <summary>
    /// 密码
    /// </summary>
    public string PassWord { get; set; } = "guest";

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// 尝试重连次数
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// 虚拟主机
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}