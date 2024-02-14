namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
/// 集成事件处理器
/// </summary>
public interface IEventHandler<in TIntegrationEvent> where TIntegrationEvent : IEvent
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task HandleAsync(TIntegrationEvent @event);
}
