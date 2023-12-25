namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 死信消息处理
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IIntegrationEventDeadLetterHandler<in TIntegrationEvent> where TIntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task HandleAsync(TIntegrationEvent @event);
}