using System.Collections.Concurrent;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint poolCount) : IChannelPool
{
    private readonly ConcurrentBag<IChannel> _channels = [];
    private int _currentCount; // 使用原子计数器来跟踪池中的通道数量

    private bool _disposed; // To detect redundant calls

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
        // 在32和64位平台上,对int类型的读取操作都是原子的,所以不需要Interlocked.Read(ref _currentCount)
        if (_currentCount >= poolCount)
        {
            await channel.CloseAsync();
            channel.Dispose();
        }
        else
        {
            _channels.Add(channel);
            Interlocked.Increment(ref _currentCount); // 安全地增加计数
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