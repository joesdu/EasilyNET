using EasilyNET.Ipc.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// A hosted service that manages the lifecycle of an IPC (Inter-Process Communication) command handler.
/// </summary>
/// <remarks>
/// This service is designed to run in the background, processing IPC commands using the provided
/// command handler implementation. It integrates with the ASP.NET Core hosting infrastructure and ensures
/// proper initialization and cleanup of the command handler, including support for asynchronous lifecycles if the
/// handler implements <see cref="IIpcLifetime" />.
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="IpcCommandHandlerHostedService" /> class.
/// </remarks>
/// <param name="commandHandler">The IPC command handler responsible for processing inter-process communication commands.</param>
/// <param name="logger">The logger used to log diagnostic and operational information for the hosted service.</param>
public sealed class IpcCommandHandlerHostedService(IpcCommandHandler commandHandler, ILogger<IpcCommandHandlerHostedService> logger) : BackgroundService
{
    private readonly ILogger _logger = logger;

    /// <summary>
    /// Executes the background service logic for processing IPC commands.
    /// </summary>
    /// <remarks>
    /// This method initializes the IPC command handler if it implements <see cref="IIpcLifetime" />,
    /// starts its asynchronous lifecycle, and then enters an infinite delay loop until the service is stopped. If the
    /// service is stopped via the <paramref name="stoppingToken" />, an <see cref="OperationCanceledException" />  is
    /// caught and logged. Any other exceptions are logged and rethrown.
    /// </remarks>
    /// <param name="stoppingToken">A <see cref="CancellationToken" /> that is triggered when the service is stopping.</param>
    /// <returns></returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IPC 命令处理后台服务开始启动");
        try
        {
            if (commandHandler is IIpcLifetime asyncLifetime)
            {
                await asyncLifetime.StartAsync();
                _logger.LogInformation("IPC 命令处理器已启动");
            }
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("IPC 命令处理后台服务正在停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IPC 命令处理后台服务发生异常");
            throw;
        }
    }

    /// <summary>
    /// Stops the IPC command processing background service asynchronously.
    /// </summary>
    /// <remarks>
    /// This method ensures that any resources associated with the IPC command handler are properly
    /// released  and that the base service stop logic is executed. If the command handler implements
    /// <see
    ///     cref="IIpcLifetime" />
    /// ,  its asynchronous stop logic will also be invoked.
    /// </remarks>
    /// <param name="cancellationToken">A <see cref="CancellationToken" /> that can be used to signal the operation should be canceled.</param>
    /// <returns></returns>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("正在停止 IPC 命令处理后台服务");
        try
        {
            if (commandHandler is IIpcLifetime asyncLifetime)
            {
                await asyncLifetime.StopAsync();
            }
            await base.StopAsync(cancellationToken);
            _logger.LogInformation("IPC 命令处理后台服务已停止");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "停止 IPC 命令处理后台服务时发生异常");
            throw;
        }
    }
}