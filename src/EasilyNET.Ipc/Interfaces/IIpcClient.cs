using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines the contract for an inter-process communication (IPC) client that can send commands and receive responses.IPC 客户端接口
/// </summary>
/// <remarks>
/// Implementations of this interface are responsible for managing the communication with an IPC channel.
/// Ensure proper disposal of the client to release any resources associated with the communication channel.
/// </remarks>
public interface IIpcClient : IDisposable
{
    /// <summary>
    /// Sends an inter-process communication (IPC) command asynchronously and waits for a response.发送 IPC 命令并等待响应
    /// </summary>
    /// <remarks>
    /// This method is used to send commands to an IPC channel and retrieve their responses. Ensure that the
    /// <paramref name="command" /> is properly constructed and that the timeout value is appropriate for the expected
    /// response time.
    /// </remarks>
    /// <param name="command">The IPC command to send. This parameter cannot be null.</param>
    /// <param name="timeout">
    /// The maximum duration to wait for a response. If not specified, a default timeout is used. A value of
    /// <see
    ///     cref="TimeSpan.Zero" />
    /// indicates no timeout.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IpcCommandResponse{TData}" />
    /// containing the response to the command, or <see langword="null" /> if no response is received within the timeout
    /// period.
    /// </returns>
    Task<IpcCommandResponse<object>?> SendCommandAsync(IIpcCommandBase command, TimeSpan timeout = default);
}