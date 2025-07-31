using System.Collections.Concurrent;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using EasilyNET.Ipc.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;

namespace EasilyNET.Ipc.Services;

/// <summary>
/// Provides a service for sending and receiving inter-process communication (IPC) commands with support for retry
/// policies, timeouts, and transport pooling.
/// </summary>
/// <remarks>
/// This service is designed to facilitate reliable IPC communication by managing a pool of transport
/// connections and applying resilience strategies such as retries and timeouts. It supports both Windows and Linux
/// platforms, using named pipes or Unix sockets respectively.
/// </remarks>
public sealed class IpcCommandService : IIpcCommandService, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly ILogger? _logger;
    private readonly IpcOptions _options;
    private readonly ResiliencePipeline _retryPipeline;
    private readonly IIpcSerializer _serializer;
    private readonly ConcurrentBag<IIpcTransport> _transportPool = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="IpcCommandService" /> class, which provides inter-process
    /// communication (IPC) services with configurable retry and timeout policies.
    /// </summary>
    /// <remarks>
    /// This constructor initializes the IPC service with a retry pipeline that handles specific
    /// exceptions, such as <see cref="IOException" />, <see cref="TimeoutException" />, and
    /// <see
    ///     cref="TaskCanceledException" />
    /// . The retry behavior, including the number of attempts, delay, and backoff type,
    /// is determined by the provided <paramref name="options" />.  The service also configures a timeout policy for IPC
    /// operations and initializes a transport pool for managing IPC connections.
    /// </remarks>
    /// <param name="options">
    /// The configuration options for the IPC service, including retry policies, timeouts, and other settings. Cannot be
    /// null.
    /// </param>
    /// <param name="serializer">The serializer used for serializing and deserializing IPC messages. Cannot be null.</param>
    /// <param name="logger">An optional logger instance for logging diagnostic and operational information.</param>
    public IpcCommandService(IOptions<IpcOptions> options, IIpcSerializer serializer, ILogger<IpcCommandService>? logger = null)
    {
        _options = options.Value;
        _serializer = serializer;
        _logger = logger;
        var retryOptions = _options.RetryPolicy;
        _retryPipeline = new ResiliencePipelineBuilder()
                         .AddRetry(new()
                         {
                             ShouldHandle = new PredicateBuilder()
                                            .Handle<IOException>()
                                            .Handle<TimeoutException>()
                                            .Handle<TaskCanceledException>(),
                             MaxRetryAttempts = retryOptions.MaxAttempts,
                             Delay = retryOptions.InitialDelay,
                             BackoffType = retryOptions.BackoffType.Equals("Linear", StringComparison.OrdinalIgnoreCase)
                                               ? DelayBackoffType.Linear
                                               : DelayBackoffType.Exponential,
                             UseJitter = retryOptions.UseJitter,
                             OnRetry = args =>
                             {
                                 if (_logger is not null && _logger.IsEnabled(LogLevel.Warning))
                                 {
                                     _logger.LogWarning("重试 IPC 操作，尝试次数: {AttemptNumber}, 异常: {Exception}", args.AttemptNumber, args.Outcome.Exception?.Message);
                                 }
                                 return ValueTask.CompletedTask;
                             }
                         })
                         .AddTimeout(_options.Timeout.Ipc)
                         .Build();
        InitializeTransportPool();
        if (_logger is not null && _logger.IsEnabled(LogLevel.Information))
        {
            _logger?.LogInformation("IPC 服务已初始化，传输池大小: {Size}", _options.ClientPipePoolSize);
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _cts.Cancel();
        _cts.Dispose();
        foreach (var transport in _transportPool)
        {
            transport.Dispose();
        }
        _transportPool.Clear();
        _disposed = true;
    }

    /// <inheritdoc />
    public async Task<IpcCommandResponse?> SendAndReceiveAsync(IpcCommand command, TimeSpan timeout = default)
    {
        if (timeout == TimeSpan.Zero)
        {
            timeout = _options.Timeout.Ipc;
        }
        try
        {
            return await _retryPipeline.ExecuteAsync(async _ =>
            {
                IIpcTransport? transport = null;
                try
                {
                    if (!_transportPool.TryTake(out transport))
                    {
                        transport = CreateTransport(false);
                    }
                    if (!transport.IsConnected)
                    {
                        await transport.ConnectAsync(timeout, _cts.Token);
                    }
                    var commandData = _serializer.SerializeCommand(command);
                    await transport.WriteAsync(commandData, _cts.Token);
                    var responseData = await transport.ReadAsync(_cts.Token);
                    var response = _serializer.DeserializeResponse(responseData);
                    if (_logger is not null && _logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("收到 IPC 响应: {CommandId}", command.CommandId);
                    }
                    return response;
                }
                finally
                {
                    if (transport != null)
                    {
                        if (transport.IsConnected)
                        {
                            _transportPool.Add(transport);
                        }
                        else
                        {
                            transport.Dispose();
                        }
                    }
                }
            }, _cts.Token);
        }
        catch (Exception ex)
        {
            if (_logger is not null && _logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "发送 IPC 命令失败，CommandId: {CommandId}", command.CommandId);
            }
            return null;
        }
    }

    private void InitializeTransportPool()
    {
        for (var i = 0; i < _options.ClientPipePoolSize; i++)
        {
            var transport = CreateTransport(false);
            _transportPool.Add(transport);
        }
    }

    private IIpcTransport CreateTransport(bool isServer) =>
        OperatingSystem.IsWindows()
            ? new NamedPipeTransport(_options.PipeName, isServer, _options.MaxServerInstances, _logger)
            : OperatingSystem.IsLinux()
                ? new UnixSocketTransport(_options.UnixSocketPath, isServer, _options.MaxServerInstances, _logger)
                : throw new PlatformNotSupportedException("仅支持 Windows 和 Linux 平台");
}