using RabbitMQ.Client;

// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// Interface for a pool of channels.
/// </summary>
internal interface IChannelPool : IDisposable
{
    /// <summary>
    /// Retrieves a channel from the pool.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved channel.</returns>
    Task<IChannel> GetChannel();

    /// <summary>
    /// Returns a channel to the pool or releases it.
    /// </summary>
    /// <param name="channel">The channel to return or release.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task ReturnChannel(IChannel channel);
}