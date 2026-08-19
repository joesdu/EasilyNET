namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
///     <para xml:lang="en">Strategy used to decide where a delayed message is handed over to once its delay elapsed</para>
///     <para xml:lang="zh">延迟到期后决定消息交付位置的策略</para>
/// </summary>
public enum EDelayAddressMode
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Default. The delay address mirrors the exchange semantics of the event, so a delayed publish reaches the same consumers a
    ///     normal publish would: fanout events are handed to the target exchange, routing/topic events keep their routing key (topic
    ///     wildcards still work because the delivery exchange is a topic exchange), headers events and the default exchange fall back
    ///     to the configured queue
    ///     </para>
    ///     <para xml:lang="zh">
    ///     默认值。延迟地址镜像事件的交换机语义,因此延迟发布与普通发布抵达相同的消费者:
    ///     Fanout 事件交给目标交换机,Routing/Topics 事件保留其路由键(投递交换机本身是 topic 类型,通配符依然生效),
    ///     Headers 事件与默认交换机则回退到所配置的队列
    ///     </para>
    /// </summary>
    RoutingAware,

    /// <summary>
    ///     <para xml:lang="en">
    ///     The delayed message is delivered straight into the queue configured for the event, bypassing the target exchange. Simplest
    ///     and cheapest topology, but a fanout event will only reach its own queue
    ///     </para>
    ///     <para xml:lang="zh">
    ///     延迟消息直接投递到事件所配置的队列,绕过目标交换机。拓扑最简单、开销最低,
    ///     但 Fanout 事件只会抵达其自身队列
    ///     </para>
    /// </summary>
    QueueDirect
}
