// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.RabbitBus;

/// <summary>
/// RabbitMQ配置
/// </summary>
public class RabbitConfig
{
    /// <summary>
    /// 主机名(IP地址)
    /// </summary>
    public string Host { get; set; } = "localhost";

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
    /// 端口
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// 虚拟主机
    /// </summary>
    public string VirtualHost { get; set; } = "/";
}