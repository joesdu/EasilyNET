namespace EasilyNET.RabbitBus.Abstractions;

/// <summary>
/// 集成事件处理器
/// </summary>
public interface IIntegrationEventHandler<in TIntegrationEvent> where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task HandleAsync(TIntegrationEvent @event);
}