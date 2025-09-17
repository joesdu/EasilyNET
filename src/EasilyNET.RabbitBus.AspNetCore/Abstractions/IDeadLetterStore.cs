using EasilyNET.RabbitBus.Core.Abstraction;

namespace EasilyNET.RabbitBus.AspNetCore.Abstractions;

/// <summary>
/// 死信消息存储接口
/// </summary>
internal interface IDeadLetterStore
{
    ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default);
}

/// <summary>
/// 死信消息结构
/// </summary>
internal interface IDeadLetterMessage
{
    string EventType { get; }
    string EventId { get; }
    DateTime CreatedUtc { get; }
    int RetryCount { get; }
    IEvent OriginalEvent { get; }
}

internal sealed record DeadLetterMessage(string EventType, string EventId, DateTime CreatedUtc, int RetryCount, IEvent OriginalEvent) : IDeadLetterMessage;
