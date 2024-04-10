using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using RabbitMQ.Client;
using System.Collections.Concurrent;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class ChannelPool(IConnection connection, uint poolCount) : IChannelPool, IDisposable
{
    private readonly ConcurrentBag<IChannel> _channels = [];

    private bool _disposed; // To detect redundant calls

    /// <inheritdoc />
    public async Task<IChannel> GetChannel() => _channels.TryTake(out var channel) ? await Task.FromResult(channel) : await connection.CreateChannelAsync();

    /// <inheritdoc />
    public async Task ReturnChannel(IChannel channel)
    {
        if (_channels.Count >= poolCount)
        {
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
        if (_disposed)
            return;
        foreach (var channel in _channels)
        {
            try
            {
                channel.Dispose();
            }
            catch (Exception)
            {
                // Log the exception or handle it as needed.
            }
        }
        _disposed = true;
    }
}