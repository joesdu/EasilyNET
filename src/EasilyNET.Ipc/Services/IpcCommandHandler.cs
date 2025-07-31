using System.Collections.Concurrent;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// Handles inter-process communication (IPC) commands by managing transports, processing commands, and providing
/// responses.
/// </summary>
/// <remarks>
/// This class is responsible for starting and stopping IPC communication, managing multiple transport
/// instances,  and processing commands received from connected clients. It supports retry policies and timeout
/// configurations  for robust communication handling. The class is designed to be used in scenarios where reliable IPC
/// is required,  such as between different processes or services on the same machine.  The
/// <see
///     cref="IpcCommandHandler" />
/// implements <see cref="IIpcCommandHandler" />, <see cref="IIpcLifetime" />,  and
/// <see
///     cref="IDisposable" />
/// to provide a complete lifecycle management for IPC operations.
/// </remarks>
public sealed class IpcCommandHandler : IIpcCommandHandler, IIpcLifetime, IDisposable
{
    private readonly ILogger _logger;
    private readonly IpcOptions _options;
    private readonly ConcurrentDictionary<string, DateTime> _processedCommands = new();
    private readonly IIpcSerializer _serializer;
    private readonly List<IIpcTransport> _transports = [];
    private CancellationTokenSource? _cts;
    private bool _disposed;
    private bool _isStarted;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcCommandHandler" /> class, configuring the IPC command handling
    /// pipeline with retry and timeout policies.
    /// </summary>
    /// <remarks>
    /// This constructor sets up a resilience pipeline for handling IPC commands, including retry and
    /// timeout policies. The retry policy handles <see cref="IOException" /> and <see cref="TimeoutException" />
    /// exceptions, with configurable retry attempts, delay, and backoff type. A periodic cleanup operation is also
    /// initialized to manage processed commands.
    /// </remarks>
    /// <param name="options">The IPC options used to configure retry policies, timeouts, and other settings. Cannot be null.</param>
    /// <param name="serializer">The serializer used for IPC message serialization and deserialization. Cannot be null.</param>
    /// <param name="logger">The logger used for logging IPC operations and retry attempts. Cannot be null.</param>
    public IpcCommandHandler(IOptions<IpcOptions> options, IIpcSerializer serializer, ILogger<IpcCommandHandler> logger)
    {
        _options = options.Value;
        _serializer = serializer;
        _logger = logger;
        //var retryOptions = _options.RetryPolicy;
        //new ResiliencePipelineBuilder()
        //    .AddRetry(new()
        //    {
        //        ShouldHandle = new PredicateBuilder()
        //                       .Handle<IOException>()
        //                       .Handle<TimeoutException>(),
        //        MaxRetryAttempts = retryOptions.MaxAttempts,
        //        Delay = retryOptions.InitialDelay,
        //        BackoffType = retryOptions.BackoffType.Equals("Linear", StringComparison.OrdinalIgnoreCase)
        //                          ? DelayBackoffType.Linear
        //                          : DelayBackoffType.Exponential,
        //        OnRetry = args =>
        //        {
        //            _logger.LogWarning("重试 IPC 操作，尝试次数: {AttemptNumber}, 异常: {Exception}", args.AttemptNumber, args.Outcome.Exception?.Message);
        //            return ValueTask.CompletedTask;
        //        }
        //    })
        //    .AddTimeout(_options.Timeout.Ipc)
        //    .Build();
        _ = new Timer(CleanupProcessedCommands, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _cts?.Cancel();
        _cts?.Dispose();
        foreach (var transport in _transports)
        {
            transport.Dispose();
        }
        _transports.Clear();
        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command)
    {
        var response = new IpcCommandResponse
        {
            CommandId = command.CommandId,
            Success = false,
            Message = "未注册命令处理器，请实现 IIpcCommandHandler"
        };
        return await Task.FromResult(response);
    }

    /// <inheritdoc />
    public async Task StartAsync()
    {
        if (_isStarted)
        {
            return;
        }
        _cts = new();
        _isStarted = true;
        for (var i = 0; i < _options.MaxServerInstances; i++)
        {
            var transport = CreateTransport(true);
            _transports.Add(transport);
            _ = Task.Run(() => ListenForCommandsAsync(transport, _cts.Token));
        }
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("IPC 命令处理器启动成功，管道/套接字实例数: {Count}", _options.MaxServerInstances);
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        if (!_isStarted)
        {
            return;
        }
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }
        foreach (var transport in _transports)
        {
            transport.Disconnect();
            transport.Dispose();
        }
        _transports.Clear();
        _isStarted = false;
        _logger.LogInformation("IPC 命令处理器已停止");
    }

    private void CleanupProcessedCommands(object? state)
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-5);
        var expiredKeys = _processedCommands.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToArray();
        foreach (var key in expiredKeys)
        {
            _processedCommands.TryRemove(key, out _);
        }
    }

    private async Task ListenForCommandsAsync(IIpcTransport transport, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!transport.IsConnected)
                {
                    await transport.WaitForConnectionAsync(cancellationToken);
                }
                var data = await transport.ReadAsync(cancellationToken);
                var command = _serializer.DeserializeCommand(data);
                if (command != null && _processedCommands.TryAdd(command.CommandId, DateTime.UtcNow))
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("处理 IPC 命令: {CommandType}, CommandId: {CommandId}", command.CommandType, command.CommandId);
                    }
                    var response = await HandleCommandAsync(command);
                    var responseData = _serializer.SerializeResponse(response);
                    await transport.WriteAsync(responseData, cancellationToken);
                }
                if (transport.IsConnected)
                {
                    continue;
                }
                transport.Disconnect();
                await transport.WaitForConnectionAsync(cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("IPC 命令监听被取消");
            }
        }
        catch (IOException ioEx)
        {
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("IPC 管道/套接字断开: {Error}", ioEx.Message);
            }
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "处理 IPC 命令时发生错误");
            }
        }
        finally
        {
            transport.Disconnect();
            transport.Dispose();
        }
    }

    private IIpcTransport CreateTransport(bool isServer) =>
        OperatingSystem.IsWindows()
            ? new NamedPipeTransport(_options.PipeName, isServer, _options.MaxServerInstances, _logger)
            : OperatingSystem.IsLinux()
                ? new UnixSocketTransport(_options.UnixSocketPath, isServer, _options.MaxServerInstances, _logger)
                : throw new PlatformNotSupportedException("仅支持 Windows 和 Linux 平台");
}