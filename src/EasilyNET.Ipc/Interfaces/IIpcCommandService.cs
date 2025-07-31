using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines a service for sending inter-process communication (IPC) commands and receiving responses.
/// </summary>
/// <remarks>
/// This interface provides a mechanism for asynchronous communication between processes using IPC
/// commands. Implementations of this interface are expected to handle the transmission of commands and the reception of
/// responses, including timeout management.
/// </remarks>
public interface IIpcCommandService
{
    /// <summary>
    /// Sends an IPC command to the target and waits for a response within the specified timeout period.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous and should be awaited to ensure proper handling of the
    /// response.
    /// </remarks>
    /// <param name="command">The IPC command to send. This parameter cannot be null.</param>
    /// <param name="timeout">The maximum amount of time to wait for a response. If not specified, a default timeout is used.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result is an <see cref="IpcCommandResponse" />
    /// containing the response to the command, or <see langword="null" /> if no response is received within the timeout
    /// period.
    /// </returns>
    Task<IpcCommandResponse?> SendAndReceiveAsync(IpcCommand command, TimeSpan timeout = default);
}