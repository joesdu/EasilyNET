using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
/// 集群链接配置
/// </summary>
public sealed class RabbitMultiConfig : BaseConfig
{
    /// <summary>
    /// 配置多端点
    /// </summary>
    public List<AmqpTcpEndpoint>? AmqpTcpEndpoints { get; set; } = null;
}
