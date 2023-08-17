namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 发送事件接口定义
/// </summary>
public interface IIntegrationEventBus
{
    /// <summary>
    /// 发送事件
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    /// <param name="routingKey">默认使用RabbitMQ特性上的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</param>
    /// <param name="priority">使用优先级需要先使用RabbitQueueArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</param>
    /// <param name="cancellationToken">CancellationToken</param>
    // ReSharper disable once UnusedMember.Global
    void Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IIntegrationEvent;

    /// <summary>
    /// 延时事件发送
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    /// <param name="ttl">延时时长(毫秒)</param>
    /// <param name="routingKey">默认使用RabbitMQ特性上的值,若是显式传入,则根据传入的值路由,以适配Topic模式下多路由键生产者的发信模式</param>
    /// <param name="priority">使用优先级需要先使用RabbitQueueArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置0-9之间的数值</param>
    /// <param name="cancellationToken">CancellationToken</param>
    // ReSharper disable once UnusedMember.Global
    void Publish<T>(T @event, uint ttl, string? routingKey = null, byte? priority = 0, CancellationToken? cancellationToken = null) where T : IIntegrationEvent;
}