// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// RabbitMQ QOS配置
/// <list type="number">
///     <item>第一个参数设置为0,则表示不限制消费者在接收消息时可以接收的最大字节数.这意味着,消费者可以在接收消息时接收任意大小的消息.</item>
///     <item>第二个参数是指消费者在接收消息时,每次最多可以接收多少条消息.</item>
///     <item>第三个参数设置为true,则表示限制所有消费者的消息数量,而不是每个消费者的消息数量.这意味着,如果有多个消费者连接到同一个队列,那么队列中的所有消息都将被平均分配给所有消费者.</item>
/// </list>
/// </summary>
/// <param name="prefetchSize">默认值:0,表示不做限制</param>
/// <param name="prefetchCount">默认值:1,表示一次消费一个</param>
/// <param name="global">默认值:false</param>
[AttributeUsage(AttributeTargets.Class)]
public class RabbitQosAttribute(uint prefetchSize = 0, ushort prefetchCount = 1, bool global = false) : Attribute
{
    /// <summary>
    /// 若为0,则表示不限制消费者在接收消息时可以接收的最大字节数.这意味着,消费者可以在接收消息时接收任意大小的消息
    /// </summary>
    public uint PrefetchSize { get; private set; } = prefetchSize;

    /// <summary>
    /// 指消费者在接收消息时,每次最多可以接收多少条消息
    /// </summary>
    public ushort PrefetchCount { get; private set; } = prefetchCount;

    /// <summary>
    /// 若为true,则表示限制所有消费者的消息数量,而不是每个消费者的消息数量.这意味着,如果有多个消费者连接到同一个队列,那么队列中的所有消息都将被平均分配给所有消费者.
    /// </summary>
    public bool Global { get; private set; } = global;
}