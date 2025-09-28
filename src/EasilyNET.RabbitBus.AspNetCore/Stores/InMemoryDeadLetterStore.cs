using System.Collections.Concurrent;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;

namespace EasilyNET.RabbitBus.AspNetCore.Stores;

/// <summary>
/// 简单内存死信存储(线程安全)
/// </summary>
internal sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentQueue<IDeadLetterMessage> _queue = new();

    public ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    // ReSharper disable once UnusedMember.Global
    public IReadOnlyCollection<IDeadLetterMessage> Snapshot() => [.. _queue];
}