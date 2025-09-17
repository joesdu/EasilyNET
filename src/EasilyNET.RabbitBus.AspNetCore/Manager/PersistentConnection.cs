using EasilyNET.Core.Threading;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

internal sealed class PersistentConnection : IDisposable, IAsyncDisposable
{
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

    // 新增: 确保仅存在一个重连任务
    private Task? _reconnectTask;

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ResiliencePipelineProvider<string> pp, ILogger<PersistentConnection> logger)
    {
        _resiliencePipeline = pp.GetPipeline(Constant.ResiliencePipelineName);
        connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        _logger = logger;
        _connectionFactory = connFactory;
        _options = options;

        // 初始化连接（保持同步等待以兼容现有使用方式）
        var initTask = InitializeConnectionAsync();
        _reconnectTask = initTask; // 视为当前的“连接任务”
        initTask.Wait();
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
                    _logger.LogWarning(ex, "等待重连任务完成时发生错误");
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
                    _logger.LogWarning(ex, "清理RabbitMQ通道时发生错误");
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
                    _logger.LogWarning(ex, "清理RabbitMQ连接时发生错误");
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

    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// 异步获取RabbitMQ通道
    /// </summary>
    /// <returns>RabbitMQ通道</returns>
    public async ValueTask<IChannel> GetChannelAsync() => await GetChannelInternalAsync().ConfigureAwait(false);

    private async ValueTask<IChannel> GetChannelInternalAsync()
    {
        // 1. 若已有可用通道直接返回
        var existing = _currentChannel;
        if (existing is { IsOpen: true })
        {
            return existing;
        }

        // 2. 如果连接不可用，启动/复用重连，并等待连接就绪（而不是立即抛出）
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
                _logger.LogError(ex, "创建 RabbitMQ 通道失败，将重新进入重连流程");
                // 连接可能又掉了，重新触发重连（如果还没有）
                if (_currentConnection is not { IsOpen: true })
                {
                    StartReconnectProcess();
                }
                throw; // 让调用方感知失败（可能选择重试）
            }
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化RabbitMQ连接失败，进入后台重连");
                StartReconnectProcess();
                // 不在此处重置 _connectionReadyTcs（保持未完成状态，后续重连成功设为完成）
                throw;
            }
        }
    }

    private async Task<IConnection> CreateConnectionAsync()
    {
        var _config = _options.Get(Constant.OptionName);
        var conn = _config.AmqpTcpEndpoints is not null && _config.AmqpTcpEndpoints.Count > 0
                       ? await _connectionFactory.CreateConnectionAsync(_config.AmqpTcpEndpoints, _config.ApplicationName).ConfigureAwait(false)
                       : await _connectionFactory.CreateConnectionAsync(_config.ApplicationName).ConfigureAwait(false);
        if (conn.IsOpen && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("已成功连接到RabbitMQ服务器");
        }
        return conn;
    }

    private async Task<IChannel> CreateChannelAsync()
    {
        if (_currentConnection is not { IsOpen: true })
        {
            throw new InvalidOperationException("无法在没有有效连接的情况下创建通道");
        }
        var config = _options.Get(Constant.OptionName);
        var channelOptions = new CreateChannelOptions(config.PublisherConfirms, config.PublisherConfirms);
        return await _currentConnection.CreateChannelAsync(channelOptions).ConfigureAwait(false);
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

            // 使用异步锁保护重连状态
            using (await _reconnectAsyncLock.LockAsync().ConfigureAwait(false))
            {
                if (_reconnectTask is { IsCompleted: false })
                {
                    return; // 已有重连任务在运行
                }
                if (_connectionReadyTcs.Task.IsCompleted)
                {
                    // 重置为新的未完成任务，等待重连成功时完成
                    _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }

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
                    _logger.LogInformation("尝试重新连接到RabbitMQ...");
                    IConnection? oldConn;
                    IChannel? oldCh;

                    // 先建立新连接与通道，再替换，缩短无连接窗口
                    var newConnection = await CreateConnectionAsync().ConfigureAwait(false);
                    IChannel newChannel;
                    try
                    {
                        _currentConnection = newConnection; // 临时放入以便 CreateChannelAsync 使用
                        newChannel = await CreateChannelAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "创建RabbitMQ通道失败，清理新连接");
                        // 如果创建通道失败，关闭新连接并抛出
                        try
                        {
                            await newConnection.DisposeAsync().ConfigureAwait(false);
                        }
                        catch (Exception disposeEx)
                        {
                            _logger.LogWarning(disposeEx, "清理新连接时发生错误");
                        }
                        throw;
                    }
                    using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
                    {
                        // 交换引用
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
                        _logger.LogWarning(ex, "释放旧RabbitMQ通道时发生错误");
                    }
                    try
                    {
                        if (!ReferenceEquals(oldConn, newConnection) && oldConn is not null)
                        {
                            await oldConn.DisposeAsync().ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "释放旧RabbitMQ连接时发生错误");
                    }
                    _logger.LogInformation("成功重新连接到RabbitMQ");
                    _connectionReadyTcs.TrySetResult(true);
                    ConnectionReconnected?.Invoke(this, EventArgs.Empty);
                }, cancellationToken).ConfigureAwait(false);

                // 成功则退出循环
                break;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("RabbitMQ重连操作已取消");
                break;
            }
            catch (Exception ex)
            {
                // 失败后等待再试
                if (_logger.IsEnabled(LogLevel.Warning))
                {
                    _logger.LogWarning(ex, "重连 RabbitMQ 失败，将在一段时间后继续尝试");
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(_options.Get(Constant.OptionName).ReconnectIntervalSeconds), cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("RabbitMQ重连等待被取消");
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
}