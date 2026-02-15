namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Context object passed to event middleware, containing the event, headers, and cancellation token</para>
///     <para xml:lang="zh">传递给事件中间件的上下文对象，包含事件、消息头和取消令牌</para>
/// </summary>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The event type</para>
///     <para xml:lang="zh">事件类型</para>
/// </typeparam>
public sealed class EventContext<TEvent> where TEvent : IEvent
{
    /// <summary>
    ///     <para xml:lang="en">The event instance being processed</para>
    ///     <para xml:lang="zh">正在处理的事件实例</para>
    /// </summary>
    public required TEvent Event { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Message headers from RabbitMQ</para>
    ///     <para xml:lang="zh">来自RabbitMQ的消息头</para>
    /// </summary>
    public required IReadOnlyDictionary<string, object?> Headers { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Cancellation token for cooperative cancellation</para>
    ///     <para xml:lang="zh">用于协作取消的取消令牌</para>
    /// </summary>
    public required CancellationToken CancellationToken { get; init; }
}