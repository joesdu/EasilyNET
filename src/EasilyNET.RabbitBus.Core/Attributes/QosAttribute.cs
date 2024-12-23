// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">RabbitMQ QOS configuration</para>
///     <para xml:lang="zh">RabbitMQ QOS配置</para>
///     <list type="number">
///         <item>
///             <para xml:lang="en">
///             If the first parameter is set to 0, it means that there is no limit on the maximum number of bytes that the consumer
///             can receive when receiving messages. This means that the consumer can receive messages of any size when receiving messages
///             </para>
///             <para xml:lang="zh">第一个参数设置为0,则表示不限制消费者在接收消息时可以接收的最大字节数.这意味着,消费者可以在接收消息时接收任意大小的消息</para>
///         </item>
///         <item>
///             <para xml:lang="en">The second parameter limits the maximum number of messages that the consumer can receive before acknowledgment</para>
///             <para xml:lang="zh">第二个参数是指限制消费者在确认之前可以接收的最大消息数</para>
///         </item>
///         <item>
///             <para xml:lang="en">
///             If the third parameter is set to true, it means that the number of messages is limited for all consumers, not for
///             each consumer. This means that if multiple consumers are connected to the same queue, all messages in the queue will be evenly
///             distributed to all consumers
///             </para>
///             <para xml:lang="zh">第三个参数设置为true,则表示限制所有消费者的消息数量,而不是每个消费者的消息数量.这意味着,如果有多个消费者连接到同一个队列,那么队列中的所有消息都将被平均分配给所有消费者</para>
///         </item>
///     </list>
/// </summary>
/// <param name="prefetchSize">
///     <para xml:lang="en">Default value: 0, which means no limit</para>
///     <para xml:lang="zh">默认值:0,表示不做限制</para>
/// </param>
/// <param name="prefetchCount">
///     <para xml:lang="en">
///     Limits the maximum number of messages that the consumer can receive before acknowledgment. Default value: 1, which means one
///     message at a time
///     </para>
///     <para xml:lang="zh">限制消费者在确认之前可以接收的最大消息数,默认值:1,表示一次消费一个</para>
/// </param>
/// <param name="global">
///     <para xml:lang="en">Default value: false</para>
///     <para xml:lang="zh">默认值:false</para>
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class QosAttribute(uint prefetchSize = 0, ushort prefetchCount = 1, bool global = false) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     If set to 0, it means that there is no limit on the maximum number of bytes that the consumer can receive when receiving
    ///     messages. This means that the consumer can receive messages of any size when receiving messages
    ///     </para>
    ///     <para xml:lang="zh">若为0,则表示不限制消费者在接收消息时可以接收的最大字节数.这意味着,消费者可以在接收消息时接收任意大小的消息</para>
    /// </summary>
    public uint PrefetchSize { get; private set; } = prefetchSize;

    /// <summary>
    ///     <para xml:lang="en">Limits the maximum number of messages that the consumer can receive before acknowledgment</para>
    ///     <para xml:lang="zh">指消费者在接收消息时,每次最多可以接收多少条消息</para>
    /// </summary>
    public ushort PrefetchCount { get; private set; } = prefetchCount;

    /// <summary>
    ///     <para xml:lang="en">
    ///     If set to true, it means that the number of messages is limited for all consumers, not for each consumer. This means that if
    ///     multiple consumers are connected to the same queue, all messages in the queue will be evenly distributed to all consumers
    ///     </para>
    ///     <para xml:lang="zh">若为true,则表示限制所有消费者的消息数量,而不是每个消费者的消息数量.这意味着,如果有多个消费者连接到同一个队列,那么队列中的所有消息都将被平均分配给所有消费者</para>
    /// </summary>
    public bool Global { get; private set; } = global;
}