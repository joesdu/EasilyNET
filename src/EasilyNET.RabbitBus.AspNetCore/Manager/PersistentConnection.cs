using EasilyNET.Core.Threading;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Registry;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class PersistentConnection : IDisposable
{
    private readonly AsyncLock _asyncLock = new();
    private readonly IConnectionFactory _connectionFactory;
    private readonly ILogger<PersistentConnection> _logger;
    private readonly IOptionsMonitor<RabbitConfig> _options;
    private readonly ResiliencePipeline _resiliencePipeline;
    private volatile IChannel? _currentChannel;

    // 使用volatile以确保线程间可见性
    private volatile IConnection? _currentConnection;
    private bool _disposed;
    private CancellationTokenSource _reconnectCts = new();

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ResiliencePipelineProvider<string> pp, ILogger<PersistentConnection> logger)
    {
        _resiliencePipeline = pp.GetPipeline(Constant.ResiliencePipelineName);
        connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        _logger = logger;
        _connectionFactory = connFactory;
        _options = options;
        var task = Task.Run(InitializeConnectionAsync);
        task.Wait();
    }

    // 为了兼容现有代码，保留同步属性，但内部使用异步实现
    public IChannel Channel => GetChannelAsync().AsTask().GetAwaiter().GetResult();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        try
        {
            _reconnectCts.Cancel();
            _reconnectCts.Dispose();
            _currentChannel?.Dispose();
            _currentConnection?.Dispose();
            _asyncLock.Dispose();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical("Error disposing RabbitMQ connection: {Message}", ex.Message);
            }
        }
    }

    private async ValueTask<IChannel> GetChannelAsync()
    {
        using (await _asyncLock.LockAsync())
        {
            if (_currentChannel is { IsOpen: true })
            {
                return _currentChannel;
            }
            IChannel? channel;
            try
            {
                channel = await CreateChannelAsync();
                _currentChannel = channel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "无法创建RabbitMQ通道，将尝试重连");
                StartReconnectProcess();
                throw; // 如果此时无法创建通道，让调用者知道
            }
            return channel;
        }
    }

    // 事件
    public event EventHandler? ConnectionDisconnected;

    public event EventHandler? ConnectionReconnected;

    // 初始化连接 - 异步方法
    private async Task InitializeConnectionAsync()
    {
        using (await _asyncLock.LockAsync())
        {
            if (_currentConnection is { IsOpen: true })
            {
                return;
            }
            try
            {
                await _resiliencePipeline.ExecuteAsync(async _ =>
                {
                    _currentConnection = await CreateConnectionAsync();
                    RegisterConnectionEvents();
                    _currentChannel = await CreateChannelAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化RabbitMQ连接失败");
                // 启动重连流程
                StartReconnectProcess();
                throw;
            }
        }
    }

    private async Task<IConnection> CreateConnectionAsync()
    {
        var _config = _options.Get(Constant.OptionName);
        var conn = _config.AmqpTcpEndpoints is not null && _config.AmqpTcpEndpoints.Count > 0
                       ? await _connectionFactory.CreateConnectionAsync(_config.AmqpTcpEndpoints, _config.ApplicationName)
                       : await _connectionFactory.CreateConnectionAsync(_config.ApplicationName);
        if (conn.IsOpen && _logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation("已成功连接到RabbitMQ服务器");
        }
        return conn;
    }

    private async Task<IChannel> CreateChannelAsync() =>
        _currentConnection is not { IsOpen: true }
            ? throw new InvalidOperationException("无法在没有有效连接的情况下创建通道")
            : await _currentConnection.CreateChannelAsync();

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
    /// 开始重连过程
    /// </summary>
    private void StartReconnectProcess()
    {
        if (_disposed)
        {
            return;
        }

        // 异步启动重连任务，避免阻塞当前线程
        _ = Task.Run(async () =>
        {
            using (await _asyncLock.LockAsync())
            {
                if (_disposed)
                {
                    return;
                }
                // 取消之前的重连任务（如果有）
                await _reconnectCts.CancelAsync();
                _reconnectCts.Dispose();
                _reconnectCts = new();

                // 启动后台任务进行重连
                _ = Task.Run(async () => await ExecuteReconnectWithContinuousRetryAsync(_reconnectCts.Token), _reconnectCts.Token);
            }
        });
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
        var reconnected = false;
        while (!reconnected && !cancellationToken.IsCancellationRequested && !_disposed)
        {
            try
            {
                // 使用ResiliencePipeline执行重连操作
                await _resiliencePipeline.ExecuteAsync(async _ =>
                {
                    if (_currentConnection is { IsOpen: true })
                    {
                        reconnected = true;
                        return;
                    }
                    _logger.LogInformation("尝试重新连接到RabbitMQ...");

                    // 安全关闭旧连接和通道
                    try
                    {
                        _currentChannel?.Dispose();
                        _currentConnection?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "关闭旧连接时发生错误");
                    }

                    // 创建新连接
                    using (await _asyncLock.LockAsync(cancellationToken))
                    {
                        _currentConnection = await CreateConnectionAsync();
                        RegisterConnectionEvents();
                        _currentChannel = await CreateChannelAsync();
                        _logger.LogInformation("成功重新连接到RabbitMQ");
                        ConnectionReconnected?.Invoke(this, EventArgs.Empty);
                        reconnected = true;
                    }
                }, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogInformation("RabbitMQ重连操作已取消");
                break;
            }
            catch (Exception ex)
            {
                // 如果仍然失败，我们记录后再次循环尝试
                _logger.LogError(ex, "全部重试后仍然无法连接到RabbitMQ，将在一段时间后继续尝试");
                try
                {
                    // 等待一个较长时间后再次尝试
                    await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    _logger.LogInformation("RabbitMQ重连等待被取消");
                    break;
                }
            }
        }
    }
}