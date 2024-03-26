using EasilyNET.RabbitBus.AspNetCore.Enums;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
/// RabbitMQ链接配置
/// </summary>
public sealed class RabbitConfig
{
    /// <summary>
    /// 连接字符串
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// 配置多端点,若是存在则忽略 <see cref="RabbitConfig.Host" /> 配置
    /// </summary>
    public List<AmqpTcpEndpoint>? AmqpTcpEndpoints { get; set; } = null;

    /// <summary>
    /// 主机名(IP地址)
    /// </summary>
    public string? Host { get; set; } = null;

    /// <summary>
    /// 密码
    /// </summary>
    public string PassWord { get; set; } = "guest";

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// 虚拟主机
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// 端口,默认: <see langword="5672" />
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Channel池数量,默认为: 计算机上逻辑处理器的数量
    /// </summary>
    public uint PoolCount { get; set; } = 0;

    /// <summary>
    /// 尝试重连次数,默认值:<see langword="5" />
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// 序列化器类型,默认: <seealso cref="ESerializer.TextJson" />
    /// </summary>
    public ESerializer Serializer { get; set; } = ESerializer.TextJson;
}