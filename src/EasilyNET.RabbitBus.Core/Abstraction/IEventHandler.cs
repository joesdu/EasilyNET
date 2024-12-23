namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Message consumer</para>
///     <para xml:lang="zh">消息消费者</para>
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    ///     <para xml:lang="en">Message handler</para>
    ///     <para xml:lang="zh">消息处理器</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    Task HandleAsync(TEvent @event);
}