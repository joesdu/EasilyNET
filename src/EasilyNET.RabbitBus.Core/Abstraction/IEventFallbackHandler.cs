namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">
///     Optional fallback handler invoked when all event handlers fail after retries are exhausted.
///     Returns a <see cref="ConsumerAction" /> to determine the message's fate (Ack, Nack, Requeue, or DeadLetter)
///     </para>
///     <para xml:lang="zh">
///     当所有事件处理器在重试耗尽后仍然失败时调用的可选回退处理器。
///     返回 <see cref="ConsumerAction" /> 以决定消息的命运（确认、拒绝、重新入队或死信）
///     </para>
/// </summary>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The event type this fallback handler handles</para>
///     <para xml:lang="zh">此回退处理器处理的事件类型</para>
/// </typeparam>
public interface IEventFallbackHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Called when all handlers for the event have failed after retries.
    ///     Use this to implement compensation logic, logging, or to decide the message's disposition
    ///     </para>
    ///     <para xml:lang="zh">
    ///     当事件的所有处理器在重试后仍失败时调用。
    ///     用于实现补偿逻辑、日志记录或决定消息的处置方式
    ///     </para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event that failed to be processed</para>
    ///     <para xml:lang="zh">处理失败的事件</para>
    /// </param>
    /// <param name="exception">
    ///     <para xml:lang="en">The exception that caused the failure</para>
    ///     <para xml:lang="zh">导致失败的异常</para>
    /// </param>
    /// <param name="retryCount">
    ///     <para xml:lang="en">The number of retry attempts that were made</para>
    ///     <para xml:lang="zh">已进行的重试次数</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A <see cref="ConsumerAction" /> indicating what to do with the message</para>
    ///     <para xml:lang="zh">指示如何处理消息的 <see cref="ConsumerAction" /></para>
    /// </returns>
    Task<ConsumerAction> OnFallbackAsync(TEvent @event, Exception exception, int retryCount);
}