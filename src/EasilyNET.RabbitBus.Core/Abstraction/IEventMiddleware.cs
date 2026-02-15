namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">
///     Middleware interface for event processing pipeline. Wraps the entire handler execution chain,
///     enabling cross-cutting concerns such as transactions, idempotency checks, logging, and rate limiting
///     </para>
///     <para xml:lang="zh">
///     事件处理管道的中间件接口。包裹整个处理器执行链路，
///     支持事务、幂等性检查、日志记录和限流等横切关注点
///     </para>
/// </summary>
/// <typeparam name="TEvent">
///     <para xml:lang="en">The event type this middleware handles</para>
///     <para xml:lang="zh">此中间件处理的事件类型</para>
/// </typeparam>
public interface IEventMiddleware<TEvent> where TEvent : IEvent
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Handles the event within the middleware pipeline. Call <paramref name="next" /> to proceed
    ///     to the next middleware or the handler execution chain. Not calling <paramref name="next" />
    ///     will short-circuit the pipeline (e.g., for idempotency checks)
    ///     </para>
    ///     <para xml:lang="zh">
    ///     在中间件管道中处理事件。调用 <paramref name="next" /> 以继续到下一个中间件或处理器执行链路。
    ///     不调用 <paramref name="next" /> 将短路管道（例如用于幂等性检查）
    ///     </para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">The event context containing the event, headers, and cancellation token</para>
    ///     <para xml:lang="zh">包含事件、消息头和取消令牌的事件上下文</para>
    /// </param>
    /// <param name="next">
    ///     <para xml:lang="en">Delegate to invoke the next middleware or the handler chain</para>
    ///     <para xml:lang="zh">调用下一个中间件或处理器链路的委托</para>
    /// </param>
    Task HandleAsync(EventContext<TEvent> context, Func<Task> next);
}