namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
/// 死信消息处理
/// </summary>
/// <typeparam name="TIntegrationEvent"></typeparam>
public interface IEventDeadLetterHandler<in TIntegrationEvent> where TIntegrationEvent : IEvent
{
    /// <summary>
    /// 消息处理器
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    Task HandleAsync(TIntegrationEvent @event);
}