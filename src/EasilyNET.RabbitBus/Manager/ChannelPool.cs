using EasilyNET.RabbitBus.Abstraction;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.Manager;

internal sealed class ChannelPool : IChannelPool
{
    private readonly ConcurrentBag<IModel> _channels;
    private readonly IConnection _connection;
    private readonly int _maxSize;

    public ChannelPool(IConnection connection, int maxSize)
    {
        _connection = connection;
        _maxSize = maxSize;
        _channels = new();
    }

    /// <inheritdoc />
    public IModel BorrowChannel() => _channels.TryTake(out var channel) ? channel : _connection.CreateModel();

    /// <inheritdoc />
    public void RepaidChannel(IModel channel)
    {
        if (_channels.Count <= _maxSize)
        {
            _channels.Add(channel);
            return;
        }
        channel.Dispose();
    }

    public void Dispose()
    {
        foreach (var channel in _channels)
        {
            channel.Dispose();
        }
    }
}