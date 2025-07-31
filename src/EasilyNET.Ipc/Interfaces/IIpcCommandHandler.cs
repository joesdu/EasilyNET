using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines a contract for handling inter-process communication (IPC) commands.IPC 命令处理器接口
/// </summary>
public interface IIpcCommandHandler
{
    /// <summary>
    /// Handles the specified IPC command asynchronously.处理 IPC 命令
    /// </summary>
    /// <remarks>
    /// This method processes the given IPC command and performs the appropriate action based on the
    /// command's type and data. Ensure that the <paramref name="command" /> parameter is properly initialized before
    /// calling this method.
    /// </remarks>
    /// <param name="command">The IPC command to process. Cannot be <see langword="null" />.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command);
}