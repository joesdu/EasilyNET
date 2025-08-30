using EasilyNET.RabbitBus.Core.Abstraction;

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