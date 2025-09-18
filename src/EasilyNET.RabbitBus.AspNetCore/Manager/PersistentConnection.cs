using EasilyNET.Core.Threading;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using EasilyNET.RabbitBus.AspNetCore.Metrics;
using EasilyNET.RabbitBus.AspNetCore.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class PersistentConnection : IAsyncDisposable
{
    private readonly AsyncLock _asyncLock = new();
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<PersistentConnection> _logger;
    private readonly IOptionsMonitor<RabbitConfig> _options;
    private readonly ResiliencePipeline _resiliencePipeline;

    // 用于让并发调用等待连接就绪，避免频繁抛出异常
    private TaskCompletionSource<bool> _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile IChannel? _currentChannel;

    private volatile IConnection? _currentConnection;

    private volatile bool _disposed;
    private volatile bool _eventsRegistered;
    private CancellationTokenSource _reconnectCts = new();

    // 确保仅存在一个重连任务
    private Task? _reconnectTask;

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ResiliencePipelineProvider<string> pp, ILogger<PersistentConnection> logger)
    {
        _resiliencePipeline = pp.GetPipeline(Constant.ResiliencePipelineName);
        connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        _logger = logger;
        _connectionFactory = connFactory;
        _options = options;
        var task = Task.Run(() => InitializeConnectionAsync(_reconnectCts.Token));
        task.Wait();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        try
        {
            // 取消重连任务
            await _reconnectCts.CancelAsync().ConfigureAwait(false);
            _reconnectCts.Dispose();

            // 等待重连任务完成
            if (_reconnectTask is not null)
            {
                try
                {
                    await _reconnectTask.ConfigureAwait(false);
                }
                catch (Exception ex) when (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "等待重连任务完成时发生错误");
                }
            }

            // 清理资源
            if (_currentChannel is not null)
            {
                await SafeDisposeAsync(_currentChannel, "RabbitMQ通道").ConfigureAwait(false);
                _currentChannel = null;
                RabbitBusMetrics.ActiveChannels.Add(-1);
            }
            if (_currentConnection is not null)
            {
                await SafeDisposeAsync(_currentConnection, "RabbitMQ连接").ConfigureAwait(false);
                _currentConnection = null;
                RabbitBusMetrics.ActiveConnections.Add(-1);
            }

            // 清理其他资源
            try
            {
                _asyncLock.Dispose();
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "清理{ResourceName}时发生错误", nameof(_asyncLock));
                }
            }
            _connectionReadyTcs.TrySetCanceled();
        }
        catch (Exception ex) when (_logger.IsEnabled(LogLevel.Critical))
        {
            _logger.LogCritical(ex, "清理RabbitMQ连接时发生严重错误");
        }
    }

    /// <summary>
    /// 异步获取RabbitMQ通道（共享发布者通道）
    /// </summary>
    public async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PersistentConnection));

        // 1. 若已有可用通道直接返回
        if (IsChannelHealthy())
        {
            return _currentChannel!;
        }

        // 2. 如果连接不可用，启动重连并等待
        if (!IsConnectionHealthy())
        {
            StartReconnectProcess(cancellationToken);
            try
            {
                await _connectionReadyTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw new ObjectDisposedException(nameof(PersistentConnection));
            }
        }

        // 3. 再次检查通道
        if (IsChannelHealthy())
        {
            return _currentChannel!;
        }

        // 4. 创建新通道
        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (IsChannelHealthy())
            {
                return _currentChannel!;
            }
            try
            {
                var channel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
                _currentChannel = channel;
                return channel;
            }
            catch (Exception ex) when (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "创建RabbitMQ通道失败，将重新进入重连流程");
                if (!IsConnectionHealthy())
                {
                    StartReconnectProcess(cancellationToken);
                }
                throw;
            }
        }
    }

    // 事件
    public event EventHandler? ConnectionDisconnected;

    public event EventHandler? ConnectionReconnected;

    private async Task InitializeConnectionAsync(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (IsConnectionHealthy())
            {
                _connectionReadyTcs.TrySetResult(true);
                return;
            }
            try
            {
                await _resiliencePipeline.ExecuteAsync(async (_, ct) =>
                {
                    _currentConnection = await CreateConnectionAsync(ct).ConfigureAwait(false);
                    RegisterConnectionEvents(ct);
                    _currentChannel = await CreateChannelAsync(ct).ConfigureAwait(false);
                }, cancellationToken, cancellationToken).ConfigureAwait(false);
                _connectionReadyTcs.TrySetResult(true);
                RabbitBusMetrics.ConnectionReconnects.Add(1);
                RabbitBusMetrics.ActiveConnections.Add(1);
                RabbitBusMetrics.SetConnectionState(true);
            }
            catch (Exception ex) when (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "初始化RabbitMQ连接失败，进入后台重连");
                RabbitBusMetrics.SetConnectionState(false);
                StartReconnectProcess(cancellationToken);
                throw;
            }
        }
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var cfg = GetConfig();
        var conn = cfg.AmqpTcpEndpoints is not null && cfg.AmqpTcpEndpoints.Count > 0
                       ? await _connectionFactory.CreateConnectionAsync(cfg.AmqpTcpEndpoints, cfg.ApplicationName, cancellationToken).ConfigureAwait(false)
                       : await _connectionFactory.CreateConnectionAsync(cfg.ApplicationName, cancellationToken).ConfigureAwait(false);
        if (conn.IsOpen && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("已成功连接到RabbitMQ服务器: {Host}", cfg.Host);
        }
        return conn;
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken) =>
        IsConnectionHealthy()
            ? await CreateChannelAsync(_currentConnection!, cancellationToken).ConfigureAwait(false)
            : throw new InvalidOperationException("无法在没有有效连接的情况下创建通道");

    private async Task<IChannel> CreateChannelAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var config = GetConfig();
        var channelOptions = new CreateChannelOptions(config.PublisherConfirms, config.PublisherConfirms);
        return await connection.CreateChannelAsync(channelOptions, cancellationToken).ConfigureAwait(false);
    }

    private void RegisterConnectionEvents(CancellationToken ct)
    {
        if (_currentConnection == null || _eventsRegistered)
        {
            return;
        }

        // 注意：RabbitMQ.Client不支持移除事件处理器，所以我们需要确保只在需要时注册
        // 通过检查连接是否是新的来决定是否需要重新注册事件
        _currentConnection.ConnectionShutdownAsync += async (_, args) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection shutdown, reason: {Reason}", args.ReplyText);
            }
            RabbitBusMetrics.SetConnectionState(false);
            ConnectionDisconnected?.Invoke(this, EventArgs.Empty); // 触发断开事件
            StartReconnectProcess(ct);                             // 启动重连流程
            await Task.CompletedTask;
        };
        _currentConnection.ConnectionBlockedAsync += async (_, args) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection blocked: {Reason}", args.Reason);
            }
            RabbitBusMetrics.SetConnectionState(false);
            ConnectionDisconnected?.Invoke(this, EventArgs.Empty); // 触发断开事件
            await Task.CompletedTask;
        };
        _eventsRegistered = true;
    }

    /// <summary>
    /// 开始重连过程（单任务，多调用复用）
    /// </summary>
    private async void StartReconnectProcess(CancellationToken ct)
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            // 使用异步锁保护重连状态与就绪TCS
            using (await _asyncLock.LockAsync(ct).ConfigureAwait(false))
            {
                // 重要：无论是否已有重连任务，都要先重置TCS，避免调用方继续使用已完成的TCS而不等待
                if (_connectionReadyTcs.Task.IsCompleted)
                {
                    _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                // 若已有重连在进行，直接返回（保持等待）
                if (_reconnectTask is { IsCompleted: false })
                {
                    return;
                }

                // 取消之前的重连任务（如果存在）
                if (_reconnectTask is not null)
                {
                    try
                    {
                        await _reconnectCts.CancelAsync();
                        await _reconnectTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(ex, "取消之前的重连任务时发生错误");
                        }
                    }
                    finally
                    {
                        _reconnectCts.Dispose();
                    }
                }

                // 创建新的 CancellationTokenSource 用于新任务
                _reconnectCts = new();
                _reconnectTask = Task.Run(() => ExecuteReconnectWithContinuousRetryAsync(_reconnectCts.Token), ct);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task ExecuteReconnectWithContinuousRetryAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        // 若已经恢复，直接返回
        if (IsConnectionHealthy())
        {
            _connectionReadyTcs.TrySetResult(true);
            return;
        }
        var cfg = GetConfig();
        var baseInterval = TimeSpan.FromSeconds(Math.Max(1, cfg.ReconnectIntervalSeconds));
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                await _resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (cancellationToken.IsCancellationRequested || _disposed)
                    {
                        return;
                    }
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("尝试重新连接到RabbitMQ...");
                    }

                    // 安全关闭旧连接和通道
                    IConnection? oldConn;
                    IChannel? oldCh;
                    using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
                    {
                        oldCh = _currentChannel;
                        oldConn = _currentConnection;
                        _currentChannel = null;
                        _currentConnection = null;
                    }

                    // 释放旧资源
                    if (oldCh is not null)
                    {
                        await SafeDisposeAsync(oldCh, "旧RabbitMQ通道").ConfigureAwait(false);
                        RabbitBusMetrics.ActiveChannels.Add(-1);
                    }
                    if (oldConn is not null)
                    {
                        await SafeDisposeAsync(oldConn, "旧RabbitMQ连接").ConfigureAwait(false);
                        RabbitBusMetrics.ActiveConnections.Add(-1);
                    }

                    // 创建新连接
                    var newConnection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
                    IChannel newChannel;
                    try
                    {
                        newChannel = await CreateChannelAsync(newConnection, cancellationToken).ConfigureAwait(false);
                    }
                    catch (Exception ex) when (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "创建RabbitMQ通道失败，清理新连接");
                        await SafeDisposeAsync(newConnection, "新连接").ConfigureAwait(false);
                        throw;
                    }
                    using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
                    {
                        _currentConnection = newConnection;
                        _currentChannel = newChannel;
                        _eventsRegistered = false; // 重置事件注册标志
                        RabbitBusMetrics.ActiveConnections.Add(1);
                        RabbitBusMetrics.ActiveChannels.Add(1);
                    }

                    // 事件注册
                    RegisterConnectionEvents(cancellationToken);
                    _logger.LogInformation("成功重新连接到RabbitMQ");
                    _connectionReadyTcs.TrySetResult(true);
                    ConnectionReconnected?.Invoke(this, EventArgs.Empty);
                    RabbitBusMetrics.ConnectionReconnects.Add(1);
                    RabbitBusMetrics.SetConnectionState(true);
                }, cancellationToken).ConfigureAwait(false);
                break; // 成功则退出循环
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("RabbitMQ重连操作已取消");
                }
                break;
            }
            catch (Exception ex) when (_logger.IsEnabled(LogLevel.Warning))
            {
                attempt++;
                var backoff = BackoffUtility.Exponential(Math.Min(6, attempt), baseInterval, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
                _logger.LogWarning(ex, "重连RabbitMQ失败，将在{Delay}后继续尝试(attempt={Attempt})", backoff, attempt);
                try
                {
                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation("RabbitMQ重连等待被取消");
                    }
                    break;
                }
            }
        }

        // 确保调用方不会永久等待
        if (!_connectionReadyTcs.Task.IsCompleted && (_disposed || cancellationToken.IsCancellationRequested))
        {
            _connectionReadyTcs.TrySetCanceled(cancellationToken);
        }

        // 任务结束，允许下一次启动
        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_reconnectTask?.IsCompleted ?? false)
            {
                _reconnectTask = null;
            }
        }
    }

    private async Task SafeDisposeAsync(IAsyncDisposable? disposable, string resourceName)
    {
        if (disposable is null)
        {
            return;
        }
        try
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(ex, "清理{ResourceName}时发生错误", resourceName);
            }
        }
    }

    private bool IsConnectionHealthy() => _currentConnection is { IsOpen: true };

    private bool IsChannelHealthy() => _currentChannel is { IsOpen: true };

    private RabbitConfig GetConfig() => _options.Get(Constant.OptionName);
}