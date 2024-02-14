// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
/// RabbitMQ配置(单节点)
/// </summary>
public sealed class RabbitSingleConfig : BaseConfig
{
    /// <summary>
    /// 主机名(IP地址)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// 端口
    /// </summary>
    public int Port { get; set; } = 5672;
}
