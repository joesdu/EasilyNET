using System.Collections.Concurrent;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint poolCount) : IChannelPool
{
    private readonly ConcurrentBag<IChannel> _channels = [];
    private readonly IConnection _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    private int _currentCount; // Use atomic counter to track the number of channels in the pool
    private bool _disposed;    // To detect redundant calls

    /// <inheritdoc />
    public async Task<IChannel> GetChannel()
    {
        while (_channels.TryTake(out var channel))
        {
            if (channel.IsClosed) continue;
            Interlocked.Decrement(ref _currentCount); // Safely decrement the count
            return channel;
        }
        return await _connection.CreateChannelAsync();
    }

    /// <inheritdoc />
    public async Task ReturnChannel(IChannel channel)
    {
        if (channel.IsClosed || Interlocked.Increment(ref _currentCount) > poolCount)
        {
            Interlocked.Decrement(ref _currentCount); // Safely decrement the count
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
