namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Interface definition for sending events</para>
///     <para xml:lang="zh">发送事件接口定义</para>
/// </summary>
public interface IBus
{
    /// <summary>
    ///     <para xml:lang="en">Publishes an event</para>
    ///     <para xml:lang="zh">发送事件</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">
    ///     The routing key. If not provided, the value from the RabbitMQ attribute is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用RabbitMQ特性上的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using the RabbitQueueArg attribute,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用RabbitQueueArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes a delayed event</para>
    ///     <para xml:lang="zh">延时事件发送</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="ttl">
    ///     <para xml:lang="en">The delay duration in milliseconds</para>
    ///     <para xml:lang="zh">延时时长(毫秒)</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">
    ///     The routing key. If not provided, the value from the RabbitMQ attribute is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用RabbitMQ特性上的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using the RabbitQueueArg attribute,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用RabbitQueueArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes a delayed event</para>
    ///     <para xml:lang="zh">延时事件发送</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="ttl">
    ///     <para xml:lang="en">The delay duration</para>
    ///     <para xml:lang="zh">延时时长</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">
    ///     The routing key. If not provided, the value from the RabbitMQ attribute is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用RabbitMQ特性上的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using the RabbitQueueArg attribute,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用RabbitQueueArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task Publish<T>(T @event, TimeSpan ttl, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;
}