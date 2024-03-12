namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
/// 消息消费者
/// </summary>
public interface IEventHandler<in TEvent> where TEvent : IEvent
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task HandleAsync(TEvent @event);
}
