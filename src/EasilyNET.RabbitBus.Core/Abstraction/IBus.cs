// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

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
    ///     The routing key. If not provided, the value from the event configuration is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using event configuration,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用事件配置为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes multiple events in batch</para>
    ///     <para xml:lang="zh">批量发送事件</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="events">
    ///     <para xml:lang="en">The collection of event objects</para>
    ///     <para xml:lang="zh">事件对象集合</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">
    ///     The routing key. If not provided, the value from the event configuration is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using event configuration,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用事件配置为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Publishes an event that becomes visible to consumers only after the given delay. Requires delayed delivery to be enabled
    ///     (<c>WithDelayedDelivery()</c>). The delay is rounded up to whole seconds; a non-positive delay publishes immediately
    ///     </para>
    ///     <para xml:lang="zh">
    ///     发送在指定延迟之后才对消费者可见的事件。需要先启用延迟投递(<c>WithDelayedDelivery()</c>)。
    ///     延迟会向上取整到整秒;非正数延迟等同于立即发送
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="delay">
    ///     <para xml:lang="en">How long the delivery is deferred</para>
    ///     <para xml:lang="zh">推迟投递的时长</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">The routing key. If not provided, the value from the event configuration is used</para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">The priority. Requires "x-max-priority" on the destination queue</para>
    ///     <para xml:lang="zh">优先级。需要目标队列声明了"x-max-priority"参数</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task PublishDelayed<T>(T @event, TimeSpan delay, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes an event that must not be delivered before the given point in time</para>
    ///     <para xml:lang="zh">发送在指定时间点之前不得投递的事件</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="deliverAt">
    ///     <para xml:lang="en">The earliest delivery time. A time in the past publishes immediately</para>
    ///     <para xml:lang="zh">最早投递时间。过去的时间等同于立即发送</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">The routing key. If not provided, the value from the event configuration is used</para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">The priority. Requires "x-max-priority" on the destination queue</para>
    ///     <para xml:lang="zh">优先级。需要目标队列声明了"x-max-priority"参数</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task PublishAt<T>(T @event, DateTimeOffset deliverAt, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes multiple events in batch, all sharing the same delay</para>
    ///     <para xml:lang="zh">批量发送共享同一延迟时长的事件</para>
    /// </summary>
    /// <typeparam name="T">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </typeparam>
    /// <param name="events">
    ///     <para xml:lang="en">The collection of event objects</para>
    ///     <para xml:lang="zh">事件对象集合</para>
    /// </param>
    /// <param name="delay">
    ///     <para xml:lang="en">How long the delivery is deferred</para>
    ///     <para xml:lang="zh">推迟投递的时长</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">The routing key. If not provided, the value from the event configuration is used</para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">The priority. Requires "x-max-priority" on the destination queue</para>
    ///     <para xml:lang="zh">优先级。需要目标队列声明了"x-max-priority"参数</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task PublishDelayedBatch<T>(IEnumerable<T> events, TimeSpan delay, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default) where T : IEvent;

    /// <summary>
    ///     <para xml:lang="en">Publishes a delayed event (non-generic)</para>
    ///     <para xml:lang="zh">发送延迟事件 (非泛型)</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="eventType">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </param>
    /// <param name="delay">
    ///     <para xml:lang="en">How long the delivery is deferred</para>
    ///     <para xml:lang="zh">推迟投递的时长</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">The routing key. If not provided, the value from the event configuration is used</para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">The priority. Requires "x-max-priority" on the destination queue</para>
    ///     <para xml:lang="zh">优先级。需要目标队列声明了"x-max-priority"参数</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task PublishDelayed(object @event, Type eventType, TimeSpan delay, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Publishes an event (non-generic)</para>
    ///     <para xml:lang="zh">发送事件 (非泛型)</para>
    /// </summary>
    /// <param name="event">
    ///     <para xml:lang="en">The event object</para>
    ///     <para xml:lang="zh">事件对象</para>
    /// </param>
    /// <param name="eventType">
    ///     <para xml:lang="en">The type of the event</para>
    ///     <para xml:lang="zh">事件类型</para>
    /// </param>
    /// <param name="routingKey">
    ///     <para xml:lang="en">
    ///     The routing key. If not provided, the value from the event configuration is used. If provided, the event is routed based on
    ///     this value to support multi-routing key producers in Topic mode
    ///     </para>
    ///     <para xml:lang="zh">路由键。默认使用事件配置中的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</para>
    /// </param>
    /// <param name="priority">
    ///     <para xml:lang="en">
    ///     The priority. To use priority, declare the "x-max-priority" parameter for the queue using event configuration,
    ///     otherwise it will not take effect. It is recommended to set a value between 0-9
    ///     </para>
    ///     <para xml:lang="zh">优先级。使用优先级需要先使用事件配置为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</para>
    /// </param>
    /// <param name="headers">
    ///     <para xml:lang="en">Optional per-message headers, merged over (and overriding) the event configuration headers.</para>
    ///     <para xml:lang="zh">可选的逐条消息头,会合并并覆盖事件配置中的静态消息头。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">CancellationToken</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task Publish(object @event, Type eventType, string? routingKey = null, byte? priority = 0, IReadOnlyDictionary<string, object?>? headers = null, CancellationToken cancellationToken = default);
}