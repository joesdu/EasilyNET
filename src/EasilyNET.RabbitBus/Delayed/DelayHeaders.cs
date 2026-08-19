namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
///     <para xml:lang="en">Message headers attached to delayed messages. They are informational only: the ladder itself routes purely on the routing key</para>
///     <para xml:lang="zh">延迟消息上附加的消息头。它们仅用于诊断:阶梯本身完全依靠路由键进行路由</para>
/// </summary>
public static class DelayHeaders
{
    /// <summary>
    ///     <para xml:lang="en">Requested delay in seconds</para>
    ///     <para xml:lang="zh">请求的延迟秒数</para>
    /// </summary>
    public const string DelaySeconds = "x-easilynet-delay-seconds";

    /// <summary>
    ///     <para xml:lang="en">Expected delivery time (UTC, round-trip "O" format)</para>
    ///     <para xml:lang="zh">期望投递时间(UTC,往返"O"格式)</para>
    /// </summary>
    public const string DeliverAt = "x-easilynet-deliver-at";

    /// <summary>
    ///     <para xml:lang="en">Delay address the message was routed to</para>
    ///     <para xml:lang="zh">消息所路由到的延迟地址</para>
    /// </summary>
    public const string DelayAddress = "x-easilynet-delay-address";
}
