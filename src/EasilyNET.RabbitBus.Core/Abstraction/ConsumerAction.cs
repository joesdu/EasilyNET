namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Determines the action to take after a consumer fallback handler completes</para>
///     <para xml:lang="zh">消费者回退处理器完成后要执行的操作</para>
/// </summary>
public enum ConsumerAction
{
    /// <summary>
    ///     <para xml:lang="en">Acknowledge the message (mark as consumed even if handler failed)</para>
    ///     <para xml:lang="zh">确认消息（即使处理器失败也标记为已消费）</para>
    /// </summary>
    Ack,

    /// <summary>
    ///     <para xml:lang="en">Reject the message without requeuing</para>
    ///     <para xml:lang="zh">拒绝消息且不重新入队</para>
    /// </summary>
    Nack,

    /// <summary>
    ///     <para xml:lang="en">Reject the message and requeue it for later consumption</para>
    ///     <para xml:lang="zh">拒绝消息并重新入队以便稍后消费</para>
    /// </summary>
    Requeue,

    /// <summary>
    ///     <para xml:lang="en">Send the message directly to the dead letter store</para>
    ///     <para xml:lang="zh">将消息直接发送到死信存储</para>
    /// </summary>
    DeadLetter
}