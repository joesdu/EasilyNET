using EasilyNET.RabbitBus.Core.Abstraction;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.AspNetCore.Abstractions;

/// <summary>
/// 死信消息存储接口（Public）
/// </summary>
public interface IDeadLetterStore
{
    /// <summary>
    /// 存储死信消息
    /// </summary>
    ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取所有死信消息（异步枚举）
    /// </summary>
    IAsyncEnumerable<IDeadLetterMessage> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 清空所有死信消息
    /// </summary>
    ValueTask ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
///     <para xml:lang="en">Dead letter message structure (Public)</para>
///     <para xml:lang="zh">死信消息结构（Public）</para>
/// </summary>
public interface IDeadLetterMessage
{
    /// <summary>
    ///     <para xml:lang="en">Gets the event type name</para>
    ///     <para xml:lang="zh">事件类型名称</para>
    /// </summary>
    string EventType { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the unique event identifier</para>
    ///     <para xml:lang="zh">事件唯一标识</para>
    /// </summary>
    string EventId { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the UTC timestamp when the message was created</para>
    ///     <para xml:lang="zh">消息创建时间（UTC）</para>
    /// </summary>
    DateTime CreatedUtc { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the number of retry attempts before becoming dead letter</para>
    ///     <para xml:lang="zh">进入死信前的重试次数</para>
    /// </summary>
    int RetryCount { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the original event instance</para>
    ///     <para xml:lang="zh">原始事件实例</para>
    /// </summary>
    IEvent OriginalEvent { get; }
}

internal sealed record DeadLetterMessage(string EventType, string EventId, DateTime CreatedUtc, int RetryCount, IEvent OriginalEvent) : IDeadLetterMessage;