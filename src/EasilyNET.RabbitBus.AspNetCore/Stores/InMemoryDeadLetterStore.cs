using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using EasilyNET.RabbitBus.AspNetCore.Abstractions;

namespace EasilyNET.RabbitBus.AspNetCore.Stores;

/// <summary>
/// 简单内存死信存储（线程安全）
/// </summary>
internal sealed class InMemoryDeadLetterStore : IDeadLetterStore
{
    private readonly ConcurrentQueue<IDeadLetterMessage> _queue = new();

    public ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        _queue.Enqueue(message);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 获取所有死信消息（异步枚举）
    /// </summary>
    public async IAsyncEnumerable<IDeadLetterMessage> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 将队列转换为数组用于枚举（避免在枚举时修改队列）
        var messages = _queue.ToArray();
        foreach (var msg in messages)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                yield break;
            }
            yield return msg;
        }
        await Task.CompletedTask;
    }

    public ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        while (_queue.TryDequeue(out _))
        {
            // 清空队列
        }
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// 获取快照（只读集合）
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public IReadOnlyCollection<IDeadLetterMessage> Snapshot() => _queue.ToList().AsReadOnly();
}