namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// Defines the lifecycle operations for an inter-process communication (IPC) component.
/// </summary>
/// <remarks>
/// This interface provides methods to start and stop the IPC component. Implementations are responsible
/// for managing the underlying resources and ensuring proper initialization and cleanup during the lifecycle
/// transitions.
/// </remarks>
public interface IIpcLifetime
{
    /// <summary>
    /// Starts the asynchronous operation for the service.
    /// </summary>
    /// <remarks>
    /// This method initiates the service's startup process and runs asynchronously.  Callers should
    /// await the returned task to ensure the operation completes before proceeding.
    /// </remarks>
    /// <returns>A <see cref="Task" /> that represents the asynchronous operation.</returns>
    Task StartAsync();

    /// <summary>
    /// Asynchronously stops the operation or service, performing any necessary cleanup.
    /// </summary>
    /// <remarks>
    /// This method should be called to gracefully shut down the operation or service.  It ensures
    /// that any resources are released and any pending tasks are completed.
    /// </remarks>
    /// <returns>A task that represents the asynchronous stop operation.</returns>
    Task StopAsync();
}