using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint maxSize) : IChannelPool
{
    private readonly ConcurrentBag<IChannel> _channels = [];

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }
    }

    /// <inheritdoc />
    public IChannel GetChannel() => _channels.TryTake(out var channel) ? channel : connection.CreateChannel();

    /// <inheritdoc />
    public void ReturnChannel(IChannel channel)
    {
        if (_channels.Count <= maxSize)
        {
            _channels.Add(channel);
            return;
        }
        channel.Dispose();
    }
}