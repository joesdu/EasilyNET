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

internal sealed class PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ResiliencePipelineProvider<string> pp, ILogger<PersistentConnection> logger) : IAsyncDisposable
{
    private readonly AsyncLock _asyncLock = new();
    private readonly ResiliencePipeline _connectionPipeline = pp.GetPipeline(Constant.ConnectionPipelineName);
    private readonly RabbitConfig config = options.Get(Constant.OptionName);

    // 用于让并发调用等待连接就绪，避免频繁抛出异常
    private TaskCompletionSource<bool> _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private SemaphoreSlim? _consumerChannelSlots;
    private volatile IChannel? _currentChannel;

    private volatile IConnection? _currentConnection;

    private volatile bool _disposed;
    private volatile bool _eventsRegistered;
    private CancellationTokenSource _reconnectCts = new();

    // 确保仅存在一个重连任务
    private Task? _reconnectTask;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        // 1. 立即取消所有正在进行的重连尝试
        try
        {
            await _reconnectCts.CancelAsync().ConfigureAwait(false);
        }
        catch
        {
            // ignore
        }

        // 2. 尽早断开事件注册，避免在后续清理过程中触发回调
        if (_currentConnection is not null && _eventsRegistered)
        {
            try
            {
                // 注意：RabbitMQ Client 的事件移除可能不完全可靠（如果是匿名委托），
                // 但我们已经在事件处理器内部加了 _disposed 检查作为双重保障。
                _currentConnection.ConnectionShutdownAsync -= null;
                _currentConnection.ConnectionBlockedAsync -= null;
            }
            catch
            {
                // ignore
            }
            _eventsRegistered = false;
        }
        try
        {
            // 3. 等待重连任务完成
            if (_reconnectTask is not null)
            {
                try
                {
                    await _reconnectTask.ConfigureAwait(false);
                }
                catch (Exception ex) when (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "等待重连任务完成时发生错误");
                }
            }
            _reconnectCts.Dispose();

            // 4. 清理资源
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

            // 让等待方尽快结束
            _connectionReadyTcs.TrySetCanceled();
            try
            {
                _consumerChannelSlots?.Dispose();
            }
            catch
            {
                // ignore
            }
        }
        catch (Exception ex) when (logger.IsEnabled(LogLevel.Critical))
        {
            logger.LogCritical(ex, "清理RabbitMQ连接时发生严重错误");
        }
        finally
        {
            // 最后清理锁，确保所有可能使用锁的操作都已停止
            try
            {
                _asyncLock.Dispose();
            }
            catch (Exception ex)
            {
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "清理{ResourceName}时发生错误", nameof(_asyncLock));
                }
            }
        }
    }

    /// <summary>
    /// 异步获取RabbitMQ通道（共享发布者通道）
    /// </summary>
    public async ValueTask<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PersistentConnection));

        // 1. 若已有可用通道直接返回 (快速路径，无锁)
        var channel = _currentChannel;
        if (channel is { IsOpen: true })
        {
            return channel;
        }

        // 2. 如果连接不可用，启动重连并等待
        if (_currentConnection is not { IsOpen: true })
        {
            await StartReconnectProcess(cancellationToken);
            try
            {
                await _connectionReadyTcs.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException)
            {
                throw new ObjectDisposedException(nameof(PersistentConnection));
            }
        }

        // 3. 再次检查通道 (可能在等待期间已恢复)
        channel = _currentChannel;
        if (channel is { IsOpen: true })
        {
            return channel;
        }

        // 4. 创建新通道 (加锁保护)
        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            // 双重检查
            if (_currentChannel is { IsOpen: true })
            {
                return _currentChannel;
            }
            try
            {
                channel = await CreateChannelAsync(cancellationToken).ConfigureAwait(false);
                _currentChannel = channel;
                return channel;
            }
            catch (Exception ex) when (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "创建RabbitMQ通道失败，将重新进入重连流程");
                // 如果连接也坏了，触发重连
                if (_currentConnection is not { IsOpen: true })
                {
                    // 不能在此处 await，因为当前持有锁，而 StartReconnectProcess 也需要锁，会导致死锁。
                    // 让其在后台运行，它会等待当前锁释放后执行。
                    _ = StartReconnectProcess(cancellationToken);
                }
                throw;
            }
        }
    }

    /// <summary>
    /// 为消费者创建独立通道，受限于 ConsumerChannelLimit。
    /// </summary>
    public async ValueTask<ChannelLease> CreateDedicatedChannelAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(PersistentConnection));
        var slots = _consumerChannelSlots;
        if (slots is not null)
        {
            await slots.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        try
        {
            if (_currentConnection is not { IsOpen: true })
            {
                await InitializeConnectionAsync(cancellationToken).ConfigureAwait(false);
            }
            var connection = _currentConnection ?? throw new InvalidOperationException("RabbitMQ connection is not available.");
            var channel = await CreateChannelAsync(connection, cancellationToken).ConfigureAwait(false);
            return new(channel, slots);
        }
        catch
        {
            slots?.Release();
            throw;
        }
    }

    // 事件
    public event EventHandler? ConnectionDisconnected;

    public event EventHandler? ConnectionReconnected;

    internal async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await InitializeConnectionAsync(cancellationToken);
    }

    private async Task InitializeConnectionAsync(CancellationToken cancellationToken)
    {
        using (await _asyncLock.LockAsync(cancellationToken).ConfigureAwait(false))
        {
            if (_currentConnection is { IsOpen: true })
            {
                _connectionReadyTcs.TrySetResult(true);
                return;
            }
            try
            {
                await _connectionPipeline.ExecuteAsync(async (_, ct) =>
                {
                    _currentConnection = await CreateConnectionAsync(ct).ConfigureAwait(false);
                    RegisterConnectionEvents(ct);
                    _currentChannel = await CreateChannelAsync(ct).ConfigureAwait(false);
                    if (_consumerChannelSlots is null && config.ConsumerChannelLimit > 0)
                    {
                        // 初始化槽位限制，只在首个成功连接时创建
                        _consumerChannelSlots = new(config.ConsumerChannelLimit, config.ConsumerChannelLimit);
                    }
                }, cancellationToken, cancellationToken).ConfigureAwait(false);
                _connectionReadyTcs.TrySetResult(true);
                RabbitBusMetrics.ConnectionReconnects.Add(1);
                RabbitBusMetrics.ActiveConnections.Add(1);
                RabbitBusMetrics.SetConnectionState(true);
            }
            catch (Exception ex) when (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "初始化RabbitMQ连接失败，进入后台重连");
                RabbitBusMetrics.SetConnectionState(false);
                // 不能在此处 await，因为当前持有锁，而 StartReconnectProcess 也需要锁，会导致死锁。
                _ = StartReconnectProcess(cancellationToken);
                throw;
            }
        }
    }

    private async Task<IConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        // 优先使用 AmqpTcpEndpoint 列表进行连接，这对于 DDNS 和集群环境更具弹性
        if (config.AmqpTcpEndpoints is not null && config.AmqpTcpEndpoints.Count > 0)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Attempting to connect to RabbitMQ using endpoint list...");
            }
            return await connFactory.CreateConnectionAsync(config.AmqpTcpEndpoints, config.ApplicationName, cancellationToken).ConfigureAwait(false);
        }
        // 如果未提供列表，则回退到使用单个 Host 的传统方式
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Attempting to connect to RabbitMQ using single host {Host}...", config.Host);
        }
        return await connFactory.CreateConnectionAsync(config.ApplicationName, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken) =>
        _currentConnection is { IsOpen: true }
            ? await CreateChannelAsync(_currentConnection, cancellationToken).ConfigureAwait(false)
            : throw new InvalidOperationException("无法在没有有效连接的情况下创建通道");

    private async Task<IChannel> CreateChannelAsync(IConnection connection, CancellationToken cancellationToken)
    {
        var channelOptions = new CreateChannelOptions(config.PublisherConfirms, config.PublisherConfirms);
        return await connection.CreateChannelAsync(channelOptions, cancellationToken).ConfigureAwait(false);
    }

    private void RegisterConnectionEvents(CancellationToken ct)
    {
        if (_currentConnection == null || _eventsRegistered)
        {
            return;
        }
        _currentConnection.ConnectionShutdownAsync += async (_, args) =>
        {
            if (_disposed)
            {
                return; // 尽早检查，避免在处置过程中触发重连
            }
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("RabbitMQ connection shutdown, reason: {Reason}", args.ReplyText);
            }
            RabbitBusMetrics.SetConnectionState(false);
            ConnectionDisconnected?.Invoke(this, EventArgs.Empty); // 触发断开事件
            await StartReconnectProcess(ct);                       // 启动重连流程
        };
        _currentConnection.ConnectionBlockedAsync += async (_, args) =>
        {
            if (_disposed)
            {
                return; // 尽早检查
            }
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("RabbitMQ connection blocked: {Reason}", args.Reason);
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
    private async Task StartReconnectProcess(CancellationToken ct)
    {
        if (_disposed)
        {
            return;
        }
        try
        {
            // 使用异步锁保护重连状态与就绪TCS
            using (await _asyncLock.LockAsync(ct).ConfigureAwait(false))
            {
                if (_disposed)
                {
                    return;
                }

                // 若已有重连在进行，直接返回（保持等待）
                if (_reconnectTask is { IsCompleted: false })
                {
                    return;
                }

                // 重要：无论是否已有重连任务，都要先重置TCS，避免调用方继续使用已完成的TCS而不等待
                if (_connectionReadyTcs.Task.IsCompleted)
                {
                    _connectionReadyTcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
                }

                // 取消之前的重连任务（如果存在且已完成但未清理）
                if (_reconnectTask is not null)
                {
                    try
                    {
                        await _reconnectCts.CancelAsync();
                        // 这里的 await 可能会抛出异常，需要捕获
                        await _reconnectTask.ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (logger.IsEnabled(LogLevel.Warning))
                        {
                            logger.LogWarning(ex, "清理之前的重连任务时发生错误");
                        }
                    }
                    finally
                    {
                        _reconnectCts.Dispose();
                    }
                }

                // 创建新的 CancellationTokenSource 用于新任务
                _reconnectCts = new();
                // 捕获当前上下文的 Token 和内部 Token 的组合，但重连任务主要由 _reconnectCts 控制
                _reconnectTask = Task.Run(() => ExecuteReconnectWithContinuousRetryAsync(_reconnectCts.Token), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(ex, "启动重连流程失败");
            }
        }
    }

    private async Task ExecuteReconnectWithContinuousRetryAsync(CancellationToken cancellationToken)
    {
        if (_disposed)
        {
            return;
        }
        // 若已经恢复，直接返回
        if (_currentConnection is { IsOpen: true })
        {
            _connectionReadyTcs.TrySetResult(true);
            return;
        }
        var baseInterval = TimeSpan.FromSeconds(Math.Max(1, config.ReconnectIntervalSeconds));
        var attempt = 0;
        while (!cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested || _disposed)
                {
                    return;
                }
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("尝试重新连接到RabbitMQ...");
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
                // 重连需要无限重试
                var newConnection = await CreateConnectionAsync(cancellationToken).ConfigureAwait(false);
                IChannel newChannel;
                try
                {
                    newChannel = await CreateChannelAsync(newConnection, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (logger.IsEnabled(LogLevel.Error))
                {
                    logger.LogError(ex, "创建RabbitMQ通道失败，清理新连接");
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
                logger.LogInformation("成功重新连接到RabbitMQ");
                _connectionReadyTcs.TrySetResult(true);
                ConnectionReconnected?.Invoke(this, EventArgs.Empty);
                RabbitBusMetrics.ConnectionReconnects.Add(1);
                RabbitBusMetrics.SetConnectionState(true);
                break; // 成功则退出循环
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("RabbitMQ重连操作已取消");
                }
                break;
            }
            catch (Exception ex) when (logger.IsEnabled(LogLevel.Warning))
            {
                attempt++;
                var backoff = BackoffUtility.Exponential(Math.Min(6, attempt), baseInterval, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30));
                if (logger.IsEnabled(LogLevel.Warning))
                {
                    logger.LogWarning(ex, "重连RabbitMQ失败，将在{Delay}后继续尝试(attempt={Attempt})", backoff, attempt);
                }
                try
                {
                    await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    if (logger.IsEnabled(LogLevel.Information))
                    {
                        logger.LogInformation("RabbitMQ重连等待被取消");
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
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning(ex, "清理{ResourceName}时发生错误", resourceName);
            }
        }
    }

    public sealed class ChannelLease(IChannel inner, SemaphoreSlim? slots) : IAsyncDisposable
    {
        public IChannel Channel => inner;

        public async ValueTask DisposeAsync()
        {
            try
            {
                await inner.DisposeAsync().ConfigureAwait(false);
            }
            finally
            {
                slots?.Release();
            }
        }
    }
}