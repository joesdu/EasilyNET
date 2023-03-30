namespace EasilyNET.RabbitBus.Abstractions;

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
    /// <param name="priority">使用优先级需要先使用RabbitArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置1-10之间的数值</param>
    // ReSharper disable once UnusedMember.Global
    void Publish<T>(T @event, byte? priority = 1) where T : IIntegrationEvent;

    /// <summary>
    /// 延时事件发送
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="event">事件对象</param>
    /// <param name="ttl">延时时长</param>
    /// <param name="priority">使用优先级需要先使用RabbitArg特性为队列声明"x-max-priority"参数否则也不会生效,推荐设置1-10之间的数值</param>
    // ReSharper disable once UnusedMember.Global
    void Publish<T>(T @event, uint ttl, byte? priority = 1) where T : IIntegrationEvent;
}