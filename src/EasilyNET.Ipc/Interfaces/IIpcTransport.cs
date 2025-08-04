namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines a transport mechanism for inter-process communication (IPC).
/// </summary>
/// <remarks>
/// This interface provides methods for establishing, managing, and interacting with IPC connections. It
/// supports both server-side and client-side operations, including asynchronous connection handling, data transmission,
/// and disconnection. Implementations of this interface are expected to handle connection state and ensure proper
/// resource cleanup by implementing <see cref="IDisposable" />.
/// </remarks>
public interface IIpcTransport : IDisposable
{
    /// <summary>
    /// Gets a value indicating whether the connection to the service is currently active.
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// CommandReceived event
    /// </summary>
    event Func<object, byte[], Task> CommandReceived;

    /// <summary>
    /// Waits asynchronously for a connection to be established.(Server)
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. If the token is canceled, the operation is aborted.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task completes when the connection is successfully
    /// established or the operation is canceled.
    /// </returns>
    Task WaitForConnectionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Send response async
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    Task SendResponseAsync(byte[] data);

    /// <summary>
    /// Asynchronously establishes a connection to the server within the specified timeout period.(Client)
    /// </summary>
    /// <remarks>
    /// This method attempts to establish a connection asynchronously. If the connection cannot be
    /// established within the specified timeout, a <see cref="TimeoutException" /> is thrown. Ensure that the
    /// <paramref
    ///     name="cancellationToken" />
    /// is used to handle cancellation scenarios gracefully.
    /// </remarks>
    /// <param name="timeout">The maximum duration to wait for the connection to be established. Must be a positive <see cref="TimeSpan" />.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. Passing a canceled token will immediately terminate the operation.</param>
    /// <returns>
    /// A task that represents the asynchronous connection operation. The task completes when the connection is successfully
    /// established or fails due to timeout or cancellation.
    /// </returns>
    Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously reads data from the source into a byte array.
    /// </summary>
    /// <remarks>
    /// The size of the returned byte array and the amount of data read depend on the implementation of the
    /// source.
    /// </remarks>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. If the operation is canceled, the returned task will be in a canceled
    /// state.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous read operation. The task result contains a byte array with the data read
    /// from the source.
    /// </returns>
    Task<byte[]> ReadAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Asynchronously writes the specified data to the underlying stream.
    /// </summary>
    /// <remarks>
    /// This method writes the entire contents of the <paramref name="data" /> array to the stream. If the
    /// operation is canceled via the <paramref name="cancellationToken" />, the task will be marked as canceled.
    /// </remarks>
    /// <param name="data">The byte array containing the data to write. Cannot be null or empty.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests. The operation will be canceled if the token is triggered.</param>
    /// <returns>A task that represents the asynchronous write operation. The task completes when all data has been written.</returns>
    Task WriteAsync(byte[] data, CancellationToken cancellationToken);

    /// <summary>
    /// Disconnects the current connection to the resource.
    /// </summary>
    /// <remarks>
    /// This method terminates the connection to the resource if one is currently active.  After calling this
    /// method, any operations requiring an active connection will fail  until a new connection is established.
    /// </remarks>
    void Disconnect();

    /// <summary>
    /// Start
    /// </summary>
    void Start();

    /// <summary>
    /// Stop
    /// </summary>
    void Stop();
}