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
    /// <summary>
    /// The minimum interval (in milliseconds) between reconnect attempts.
    /// Set to 5000ms (5 seconds) to avoid excessive reconnection attempts that could
    /// overwhelm the RabbitMQ server or cause resource exhaustion. This value balances
    /// responsiveness with system stability.
    /// </summary>
    private const int MinReconnectIntervalMs = 5000; // 最短重连间隔5秒

    private readonly AsyncLock _asyncLock = new();
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<PersistentConnection> _logger;
    private readonly IOptionsMonitor<RabbitConfig> _options;
    private readonly AsyncLock _reconnectAsyncLock = new();
    private readonly CancellationTokenSource _reconnectCts = new();
    private readonly ResiliencePipeline _resiliencePipeline;

    // 新增: 用于让并发调用等待连接就绪，避免频繁抛出异常
    private TaskCompletionSource<bool> _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private volatile IChannel? _currentChannel;

    // 使用volatile以确保线程间可见性
    private volatile IConnection? _currentConnection;

    private bool _disposed;

    // 新增: 重连冷却时间，避免频繁重连
    private DateTime _lastReconnectAttempt = DateTime.MinValue;

    // 新增: 确保仅存在一个重连任务
    private Task? _reconnectTask;

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ResiliencePipelineProvider<string> pp, ILogger<PersistentConnection> logger)
    {
        _resiliencePipeline = pp.GetPipeline(Constant.ResiliencePipelineName);
        connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        _logger = logger;
        _connectionFactory = connFactory;
        _options = options;

        // 异步初始化：不再阻塞构造函数，调用方通过 GetChannelAsync/事件感知就绪
        _reconnectTask = Task.Run(async () =>
        {
            try
            {
                await InitializeConnectionAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "初始连接失败，将进入后台重连");
                }
            }
        });
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
            await _reconnectCts.CancelAsync();
            _reconnectCts.Dispose();

            // 等待重连任务完成
            if (_reconnectTask is not null)
            {
                try
                {
                    await _reconnectTask.ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(ex, "等待重连任务完成时发生错误");
                    }
                }
            }

            // 异步清理资源
            if (_currentChannel is not null)
            {
                try
                {
                    await _currentChannel.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(ex, "清理RabbitMQ通道时发生错误");
                    }
                }
            }
            if (_currentConnection is not null)
            {
                try
                {
                    await _currentConnection.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(ex, "清理RabbitMQ连接时发生错误");
                    }
                }
            }
            _asyncLock.Dispose();
            _reconnectAsyncLock.Dispose();
            _connectionReadyTcs.TrySetCanceled();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical("Error disposing RabbitMQ connection: {Message}", ex.Message);
            }
        }
    }

    /// <summary>
    /// 异步获取RabbitMQ通道（共享发布者通道）
    /// </summary>
    public async ValueTask<IChannel> GetChannelAsync() => await GetChannelInternalAsync().ConfigureAwait(false);

    /// <summary>
    /// 创建一个专用通道（不与共享通道复用）。适合消费者或独立使用场景。
    /// </summary>
    public async ValueTask<IChannel> CreateDedicatedChannelAsync(CancellationToken ct = default)
    {
        // 确保连接可用
        if (_currentConnection is not { IsOpen: true })
        {
            StartReconnectProcess();
            try
            {
                await _connectionReadyTcs.Task.ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw new ObjectDisposedException(nameof(PersistentConnection));
            }
        }
        // 双重检查 + 小范围锁，避免与重连交换指针时竞争
        using (await _asyncLock.LockAsync(ct).ConfigureAwait(false))
        {
            return _currentConnection is not { IsOpen: true }
                       ? throw new InvalidOperationException("无法在没有有效连接的情况下创建通道")
                       : await CreateChannelAsync(_currentConnection).ConfigureAwait(false);
        }
    }

    private async ValueTask<IChannel> GetChannelInternalAsync()
    {
        // 使用重试循环确保在短暂断线或调试长暂停后能恢复并返回通道
        while (true)
        {
            ObjectDisposedException.ThrowIf(_disposed, nameof(PersistentConnection));

            // 1. 若已有可用通道直接返回
            var existing = _currentChannel;
            if (existing is { IsOpen: true })
            {
                return existing;
            }

            // 2. 如果连接不可用，启动/复用重连，并等待连接就绪
            if (_currentConnection is not { IsOpen: true })
            {
                StartReconnectProcess();
                try
                {
                    await _connectionReadyTcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    throw new ObjectDisposedException(nameof(PersistentConnection));
                }
            }

            // 3. 再次检查通道（可能其他线程已创建）
            existing = _currentChannel;
            if (existing is { IsOpen: true })
            {
                return existing;
            }

            // 4. 创建新通道（串行化，避免并发重复创建）
            using (await _asyncLock.LockAsync().ConfigureAwait(false))
            {
                if (_currentChannel is { IsOpen: true })
                {
                    return _currentChannel;
                }
                try
                {
                    var channel = await CreateChannelAsync().ConfigureAwait(false);
                    _currentChannel = channel;
                    return channel;
                }
                catch (Exception ex)
                {
                    if (_logger.IsEnabled(LogLevel.Error))
                    {
                        _logger.LogError(ex, "创建 RabbitMQ 通道失败，将重新进入重连流程");
                    }
                    // 连接可能又掉了，重新触发重连（如果还没有），并继续下一轮重试而不是抛出
                    if (_currentConnection is not { IsOpen: true })
                    {
                        StartReconnectProcess();
                    }
                }
            }

            // 小退避，避免紧密循环
            await Task.Delay(200).ConfigureAwait(false);
        }
    }

    // 事件
    public event EventHandler? ConnectionDisconnected;

    public event EventHandler? ConnectionReconnected;

    // 初始化连接 - 异步方法
    private async Task InitializeConnectionAsync()
    {
        using (await _asyncLock.LockAsync().ConfigureAwait(false))
        {
            if (_currentConnection is { IsOpen: true })
            {
                _connectionReadyTcs.TrySetResult(true); // 已有连接
                return;
            }
            try
            {
                await _resiliencePipeline.ExecuteAsync(async _ =>
                {
                    _currentConnection = await CreateConnectionAsync().ConfigureAwait(false);
                    RegisterConnectionEvents();
                    _currentChannel = await CreateChannelAsync().ConfigureAwait(false);
                }).ConfigureAwait(false);
                _connectionReadyTcs.TrySetResult(true);
                RabbitBusMetrics.ConnectionReconnects.Add(1); // 初次连接也计一次成功连接
                RabbitBusMetrics.SetConnectionState(true);
            }
            catch (Exception ex)
            {
                if (_logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(ex, "初始化RabbitMQ连接失败，进入后台重连");
                }
                RabbitBusMetrics.SetConnectionState(false);
                StartReconnectProcess();
                // 不在此处重置 _connectionReadyTcs（保持未完成状态，后续重连成功设为完成）
                throw;
            }
        }
    }

    private async Task<IConnection> CreateConnectionAsync()
    {
        var cfg = GetConfig();
        var conn = cfg.AmqpTcpEndpoints is not null && cfg.AmqpTcpEndpoints.Count > 0
                       ? await _connectionFactory.CreateConnectionAsync(cfg.AmqpTcpEndpoints, cfg.ApplicationName).ConfigureAwait(false)
                       : await _connectionFactory.CreateConnectionAsync(cfg.ApplicationName).ConfigureAwait(false);
        if (conn.IsOpen && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("已成功连接到RabbitMQ服务器: {Host}", cfg.Host);
        }
        return conn;
    }

    private async Task<IChannel> CreateChannelAsync() =>
        _currentConnection is not { IsOpen: true }
            ? throw new InvalidOperationException("无法在没有有效连接的情况下创建通道")
            : await CreateChannelAsync(_currentConnection).ConfigureAwait(false);

    // 新增: 使用指定连接创建通道，避免在重连期间错误引用并处置新连接
    private async Task<IChannel> CreateChannelAsync(IConnection connection)
    {
        var config = GetConfig();
        var channelOptions = new CreateChannelOptions(config.PublisherConfirms, config.PublisherConfirms);
        return await connection.CreateChannelAsync(channelOptions).ConfigureAwait(false);
    }

    private void RegisterConnectionEvents()
    {
        if (_currentConnection == null)
        {
            return;
        }
        _currentConnection.ConnectionShutdownAsync += (_, args) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection shutdown, reason: {Reason}", args.ReplyText);
            }
            RabbitBusMetrics.SetConnectionState(false);
            ConnectionDisconnected?.Invoke(this, EventArgs.Empty); // 触发断开事件
            StartReconnectProcess();                               // 启动重连流程
            return Task.CompletedTask;
        };
        _currentConnection.ConnectionBlockedAsync += (_, args) =>
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning("RabbitMQ connection blocked: {Reason}", args.Reason);
            }
            RabbitBusMetrics.SetConnectionState(false);
            ConnectionDisconnected?.Invoke(this, EventArgs.Empty); // 触发断开事件
            return Task.CompletedTask;
        };
    }

    /// <summary>
    /// 开始重连过程（单任务，多调用复用）
    /// </summary>
    private async void StartReconnectProcess()
    {
        try
        {
            if (_disposed)
            {
                return;
            }

            // 使用异步锁保护重连状态与就绪TCS
            using (await _reconnectAsyncLock.LockAsync().ConfigureAwait(false))
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

                // 检查重连冷却时间，避免频繁重连
                var timeSinceLastAttempt = DateTime.UtcNow - _lastReconnectAttempt;
                if (timeSinceLastAttempt.TotalMilliseconds < MinReconnectIntervalMs)
                {
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug("Reconnect attempt too frequent, waiting {RemainingMs}ms", MinReconnectIntervalMs - (int)timeSinceLastAttempt.TotalMilliseconds);
                    }
                    return;
                }
                _lastReconnectAttempt = DateTime.UtcNow;

                // 不频繁取消/创建 CTS，除非真正需要；保持一个长期 token（Dispose 时取消）
                _reconnectTask = Task.Run(() => ExecuteReconnectWithContinuousRetryAsync(_reconnectCts.Token));
            }
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// 使用ResiliencePipeline执行重连并持续重试，直到成功
    /// </summary>
    private async Task ExecuteReconnectWithContinuousRetryAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }

        // 若已经恢复（例如启动后快速检测）
        if (_currentConnection is { IsOpen: true })
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
                    IConnection? oldConn;
                    IChannel? oldCh;

                    // 先建立新连接与通道，再替换，缩短无连接窗口
                    var newConnection = await CreateConnectionAsync().ConfigureAwait(false);
                    IChannel newChannel;
                    try
                    {
                        // 使用新连接直接创建通道，避免依赖字段状态
                        newChannel = await CreateChannelAsync(newConnection).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Error))
                        {
                            _logger.LogError(ex, "创建RabbitMQ通道失败，清理新连接");
                        }
                        // 如果创建通道失败，关闭新连接并抛出
                        try
                        {
                            await newConnection.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            if (_logger.IsEnabled(LogLevel.Warning))
                            {
                                _logger.LogWarning(disposeEx, "清理新连接时发生错误");
                            }
                        }
                        throw;
                    }
                    using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
                    {
                        // 交换引用（注意：不要在交换前修改字段，避免误处置新连接）
                        oldCh = _currentChannel;
                        oldConn = _currentConnection;
                        _currentConnection = newConnection;
                        _currentChannel = newChannel;
                    }

                    // 事件注册（使用新连接）
                    RegisterConnectionEvents();

                    // 释放旧资源
                    try
                    {
                        if (oldCh is not null)
                        {
                            await oldCh.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(ex, "释放旧RabbitMQ通道时发生错误");
                        }
                    }
                    try
                    {
                        if (oldConn is IAsyncDisposable asyncDisposableConn)
                        {
                            await asyncDisposableConn.DisposeAsync().ConfigureAwait(false);
                        }
                        else
                        {
                            oldConn?.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        if (_logger.IsEnabled(LogLevel.Warning))
                        {
                            _logger.LogWarning(ex, "释放旧RabbitMQ连接时发生错误");
                        }
                    }
                    _logger.LogInformation("成功重新连接到RabbitMQ");
                    _connectionReadyTcs.TrySetResult(true);
                    ConnectionReconnected?.Invoke(this, EventArgs.Empty);
                    RabbitBusMetrics.ConnectionReconnects.Add(1);
                    RabbitBusMetrics.SetConnectionState(true);
                }, cancellationToken).ConfigureAwait(false);

                // 成功则退出循环
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("RabbitMQ重连操作已取消");
                }
                break;
            }
            catch (Exception ex)
            {
                // 失败退避等待
                attempt++;
                var backoff = BackoffUtility.Exponential(Math.Min(6, attempt), baseInterval, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "重连 RabbitMQ 失败，将在 {Delay} 后继续尝试 (attempt={Attempt})", backoff, attempt);
                }
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

        // 若退出循环但仍未完成且未成功，确保调用方不会永久等待（可选择不完成保持等待，再次 StartReconnectProcess 时被替换）
        if (!_connectionReadyTcs.Task.IsCompleted && (_disposed || cancellationToken.IsCancellationRequested))
        {
            _connectionReadyTcs.TrySetCanceled(cancellationToken);
        }

        // 任务结束，允许下一次启动
        using (await _reconnectAsyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_reconnectTask?.IsCompleted ?? false)
            {
                _reconnectTask = null;
            }
        }
    }

    private RabbitConfig GetConfig() => _options.Get(Constant.OptionName);
}