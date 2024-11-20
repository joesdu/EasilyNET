using System.Collections.Concurrent;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint poolCount) : IChannelPool
{
    private readonly ConcurrentBag<IChannel> _channels = [];
    private int _currentCount; // 使用原子计数器来跟踪池中的通道数量
    private bool _disposed;    // To detect redundant calls

    /// <inheritdoc />
    public async Task<IChannel> GetChannel()
    {
        if (!_channels.TryTake(out var channel)) return await connection.CreateChannelAsync();
        Interlocked.Decrement(ref _currentCount); // 安全地减少计数
        return channel;
    }

    /// <inheritdoc />
    public async Task ReturnChannel(IChannel channel)
    {
        if (Interlocked.Increment(ref _currentCount) > poolCount)
        {
            Interlocked.Decrement(ref _currentCount); // 安全地减少计数
            await channel.CloseAsync();
            channel.Dispose();
        }
        else
        {
            _channels.Add(channel);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var channel in _channels)
        {
            try
            {
                channel.Dispose();
            }
            catch
            {
                // Log the exception or handle it as needed.
            }
        }
        _disposed = true;
    }
}