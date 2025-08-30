using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Event configuration for RabbitMQ</para>
///     <para xml:lang="zh">RabbitMQ事件配置</para>
/// </summary>
public sealed class EventConfiguration
{
    /// <summary>
    ///     <para xml:lang="en">Event type</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </summary>
    public Type EventType { get; set; } = typeof(IEvent);

    /// <summary>
    ///     <para xml:lang="en">Exchange configuration</para>
    ///     <para xml:lang="zh">交换机配置</para>
    /// </summary>
    public ExchangeConfig Exchange { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">Queue configuration</para>
    ///     <para xml:lang="zh">队列配置</para>
    /// </summary>
    public QueueConfig Queue { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">QoS configuration</para>
    ///     <para xml:lang="zh">QoS配置</para>
    /// </summary>
    public QosConfig Qos { get; } = new();

    /// <summary>
    ///     <para xml:lang="en">Headers configuration</para>
    ///     <para xml:lang="zh">头部配置</para>
    /// </summary>
    public Dictionary<string, object?> Headers { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Ignored handler types</para>
    ///     <para xml:lang="zh">被忽略的处理器类型</para>
    /// </summary>
    public List<Type> IgnoredHandlers { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether to enable this event</para>
    ///     <para xml:lang="zh">是否启用此事件</para>
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether handlers should be executed sequentially to maintain order</para>
    ///     <para xml:lang="zh">是否按顺序执行处理器以保持顺序</para>
    /// </summary>
    public bool SequentialHandlerExecution { get; set; }
}

/// <summary>
///     <para xml:lang="en">Exchange configuration</para>
///     <para xml:lang="zh">交换机配置</para>
/// </summary>
public sealed class ExchangeConfig
{
    /// <summary>
    ///     <para xml:lang="en">Exchange name</para>
    ///     <para xml:lang="zh">交换机名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Exchange type</para>
    ///     <para xml:lang="zh">交换机类型</para>
    /// </summary>
    public EModel Type { get; set; } = EModel.None;

    /// <summary>
    ///     <para xml:lang="en">Routing key</para>
    ///     <para xml:lang="zh">路由键</para>
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Exchange arguments</para>
    ///     <para xml:lang="zh">交换机参数</para>
    /// </summary>
    public Dictionary<string, object?> Arguments { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether exchange is durable</para>
    ///     <para xml:lang="zh">交换机是否持久化</para>
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether exchange should auto-delete</para>
    ///     <para xml:lang="zh">交换机是否自动删除</para>
    /// </summary>
    public bool AutoDelete { get; set; }
}

/// <summary>
///     <para xml:lang="en">Queue configuration</para>
///     <para xml:lang="zh">队列配置</para>
/// </summary>
public sealed class QueueConfig
{
    /// <summary>
    ///     <para xml:lang="en">Queue name</para>
    ///     <para xml:lang="zh">队列名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Queue arguments</para>
    ///     <para xml:lang="zh">队列参数</para>
    /// </summary>
    public Dictionary<string, object?> Arguments { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether queue is durable</para>
    ///     <para xml:lang="zh">队列是否持久化</para>
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether queue is exclusive</para>
    ///     <para xml:lang="zh">队列是否独占</para>
    /// </summary>
    public bool Exclusive { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Whether queue should auto-delete</para>
    ///     <para xml:lang="zh">队列是否自动删除</para>
    /// </summary>
    public bool AutoDelete { get; set; }
}

/// <summary>
///     <para xml:lang="en">QoS configuration</para>
///     <para xml:lang="zh">QoS配置</para>
/// </summary>
public sealed class QosConfig
{
    /// <summary>
    ///     <para xml:lang="en">Prefetch size</para>
    ///     <para xml:lang="zh">预取大小</para>
    /// </summary>
    public uint PrefetchSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Prefetch count</para>
    ///     <para xml:lang="zh">预取数量</para>
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    /// <summary>
    ///     <para xml:lang="en">Whether QoS is global</para>
    ///     <para xml:lang="zh">QoS是否全局</para>
    /// </summary>
    public bool Global { get; set; }
}