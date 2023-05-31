using EasilyNET.RabbitBus.Abstraction;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.Manager;

internal sealed class ChannelPool : IChannelPool
{
    private readonly ConcurrentBag<IModel> _channels;
    private readonly IConnection _connection;
    private readonly uint _maxSize;

    public ChannelPool(IConnection connection, uint maxSize)
    {
        _connection = connection;
        _maxSize = maxSize;
        _channels = new();
    }

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }
    }

    /// <inheritdoc />
    public IModel GetChannel() => _channels.TryTake(out var channel) ? channel : _connection.CreateModel();

    /// <inheritdoc />
    public void ReturnChannel(IModel channel)
    {
        if (_channels.Count <= _maxSize)
        {
            _channels.Add(channel);
            return;
        }
        channel.Dispose();
    }
}