using EasilyNET.RabbitBus.AspNetCore.Enums;
using RabbitMQ.Client;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
/// Configuration settings for RabbitMQ connection.
/// </summary>
public sealed class RabbitConfig
{
    /// <summary>
    /// The connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Configuration for multiple endpoints. If set, the <see cref="Host" /> configuration is ignored.
    /// </summary>
    public List<AmqpTcpEndpoint>? AmqpTcpEndpoints { get; set; } = null;

    /// <summary>
    /// The hostname or IP address.
    /// </summary>
    public string? Host { get; set; } = null;

    /// <summary>
    /// The password.
    /// </summary>
    public string PassWord { get; set; } = "guest";

    /// <summary>
    /// The username.
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    /// The virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// The port number. Default is 5672.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// The number of channels in the pool. Default is the number of logical processors on the machine.
    /// </summary>
    public uint PoolCount { get; set; } = 0;

    /// <summary>
    /// The number of retry attempts. Default is 5.
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// The type of serializer. Default is <see cref="ESerializer.TextJson" />.
    /// </summary>
    public ESerializer Serializer { get; set; } = ESerializer.TextJson;
}