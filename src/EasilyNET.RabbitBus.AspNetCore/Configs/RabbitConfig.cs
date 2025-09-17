using EasilyNET.RabbitBus.AspNetCore.Serializer;
using EasilyNET.RabbitBus.Core.Abstraction;
using RabbitMQ.Client;

// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Configuration settings for RabbitMQ connection</para>
///     <para xml:lang="zh">RabbitMQ 连接的配置设置</para>
/// </summary>
public sealed class RabbitConfig
{
    /// <summary>
    ///     <para xml:lang="en">The connection string</para>
    ///     <para xml:lang="zh">连接字符串</para>
    /// </summary>
    public Uri? ConnectionString { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Configuration for multiple endpoints. If set, the <see cref="Host" /> configuration is ignored</para>
    ///     <para xml:lang="zh">多个端点的配置。如果设置了此项，则忽略 <see cref="Host" /> 配置</para>
    /// </summary>
    public List<AmqpTcpEndpoint>? AmqpTcpEndpoints { get; set; } = null;

    /// <summary>
    ///     <para xml:lang="en">The hostname or IP address</para>
    ///     <para xml:lang="zh">主机名或 IP 地址</para>
    /// </summary>
    public string? Host { get; set; }

    /// <summary>
    ///     <para xml:lang="en">The password</para>
    ///     <para xml:lang="zh">密码</para>
    /// </summary>
    public string PassWord { get; set; } = "guest";

    /// <summary>
    ///     <para xml:lang="en">The username</para>
    ///     <para xml:lang="zh">用户名</para>
    /// </summary>
    public string UserName { get; set; } = "guest";

    /// <summary>
    ///     <para xml:lang="en">The virtual host</para>
    ///     <para xml:lang="zh">虚拟主机</para>
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    ///     <para xml:lang="en">The port number. Default is 5672</para>
    ///     <para xml:lang="zh">端口号。默认是 5672</para>
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    ///     <para xml:lang="en">The number of retry attempts. Default is 5</para>
    ///     <para xml:lang="zh">重试次数。默认是 5</para>
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    ///     <para xml:lang="en">Reconnection interval in seconds. Default is 15</para>
    ///     <para xml:lang="zh">重连间隔（秒）。默认是 15</para>
    /// </summary>
    public int ReconnectIntervalSeconds { get; set; } = 15;

    /// <summary>
    ///     <para xml:lang="en">The consumer dispatch concurrency. Default is 10</para>
    ///     <para xml:lang="zh">消费者调度并发数。默认是 10</para>
    /// </summary>
    public ushort ConsumerDispatchConcurrency { get; set; } = 10;

    /// <summary>
    ///     <para xml:lang="en">Maximum degree of parallelism for event handler execution. Default is 4</para>
    ///     <para xml:lang="zh">事件处理器执行的最大并行度。默认是 4</para>
    /// </summary>
    public int HandlerMaxDegreeOfParallelism { get; set; } = 4;

    /// <summary>
    ///     <para xml:lang="en">Whether to enable publisher confirms. Default is true</para>
    ///     <para xml:lang="zh">是否启用发布者确认。默认是 true</para>
    /// </summary>
    public bool PublisherConfirms { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Maximum number of outstanding publisher confirms. Default is 1000</para>
    ///     <para xml:lang="zh">最大未确认发布者确认数量。默认是 1000</para>
    /// </summary>
    public int MaxOutstandingConfirms { get; set; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Batch size for batch publishing. Default is 100</para>
    ///     <para xml:lang="zh">批量发布的批次大小。默认是 100</para>
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Timeout for waiting publisher confirms in milliseconds. Default is 30000 (30 seconds)</para>
    ///     <para xml:lang="zh">等待发布者确认的超时时间（毫秒）。默认是 30000（30秒）</para>
    /// </summary>
    public int ConfirmTimeoutMs { get; set; } = 30000;

    /// <summary>
    ///     <para xml:lang="en">Custom serializer</para>
    ///     <para xml:lang="zh">自定义序列化器</para>
    /// </summary>
    public IBusSerializer BusSerializer { get; set; } = new TextJsonSerializer();

    /// <summary>
    ///     <para xml:lang="en">QoS configuration</para>
    ///     <para xml:lang="zh">QoS配置</para>
    /// </summary>
    public QosConfig Qos { get; } = new()
    {
        PrefetchCount = 100
    };

    /// <summary>
    ///     <para xml:lang="en">Application name for identification</para>
    ///     <para xml:lang="zh">应用程序名称用于标识</para>
    /// </summary>
    public string ApplicationName { get; set; } = "EasilyNET.RabbitBus";

    /// <summary>
    ///     <para xml:lang="en">Whether to skip exchange declaration. When true, assumes exchanges already exist with correct types. Default is false</para>
    ///     <para xml:lang="zh">是否跳过交换机声明。当为true时，假设交换机已存在且类型正确。默认是false</para>
    /// </summary>
    public bool SkipExchangeDeclare { get; set; } = false;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to validate exchange types on startup. When true, validates all configured exchanges exist with correct types.
    ///     Default is true
    ///     </para>
    ///     <para xml:lang="zh">是否在启动时验证交换机类型。当为true时，验证所有配置的交换机是否存在且类型正确。默认是true</para>
    /// </summary>
    public bool ValidateExchangesOnStartup { get; set; } = true;
}