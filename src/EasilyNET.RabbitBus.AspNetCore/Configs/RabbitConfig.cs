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
    ///     <para xml:lang="en">The maximum number of connections in the pool. Default is 10</para>
    ///     <para xml:lang="zh">连接池中的最大连接数。默认是 10</para>
    /// </summary>
    public int MaxConnections { get; set; } = 10;

    /// <summary>
    ///     <para xml:lang="en">The maximum number of channels per connection. Default is 100</para>
    ///     <para xml:lang="zh">每个连接的最大通道数。默认是 100</para>
    /// </summary>
    public int MaxChannelsPerConnection { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">The consumer dispatch concurrency. Default is 10</para>
    ///     <para xml:lang="zh">消费者调度并发数。默认是 10</para>
    /// </summary>
    public int ConsumerDispatchConcurrency { get; set; } = 10;

    /// <summary>
    ///     <para xml:lang="en">Whether to enable publisher confirms. Default is true</para>
    ///     <para xml:lang="zh">是否启用发布者确认。默认是 true</para>
    /// </summary>
    public bool PublisherConfirms { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Custom serializer</para>
    ///     <para xml:lang="zh">自定义序列化器</para>
    /// </summary>
    public IBusSerializer BusSerializer { get; set; } = new TextJsonSerializer();

    /// <summary>
    ///     <para xml:lang="en">Default QoS prefetch count. Default is 100</para>
    ///     <para xml:lang="zh">默认QoS预取计数。默认是 100</para>
    /// </summary>
    public ushort DefaultPrefetchCount { get; set; } = 100;

    /// <summary>
    ///     <para xml:lang="en">Default QoS prefetch size. Default is 0</para>
    ///     <para xml:lang="zh">默认QoS预取大小。默认是 0</para>
    /// </summary>
    public uint DefaultPrefetchSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Whether QoS is global. Default is false</para>
    ///     <para xml:lang="zh">QoS是否全局。默认是 false</para>
    /// </summary>
    public bool QosGlobal { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Application name for identification</para>
    ///     <para xml:lang="zh">应用程序名称用于标识</para>
    /// </summary>
    public string ApplicationName { get; set; } = "EasilyNET.RabbitBus";
}