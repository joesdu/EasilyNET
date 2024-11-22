using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Abstraction;

/// <summary>
/// Interface for a persistent RabbitMQ connection.
/// </summary>
internal interface IPersistentConnection : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the connection is established.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Attempts to establish a connection.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task TryConnect();

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