using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint maxSize) : IChannelPool
{
    private readonly ConcurrentBag<IModel> _channels = new();

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }
    }

    /// <inheritdoc />
    public IModel GetChannel() => _channels.TryTake(out var channel) ? channel : connection.CreateModel();

    /// <inheritdoc />
    public void ReturnChannel(IModel channel)
    {
        if (_channels.Count <= maxSize)
        {
            _channels.Add(channel);
            return;
        }
        channel.Dispose();
    }
}