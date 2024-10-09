using EasilyNET.Core.Threading;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// RabbitMQ持久链接
/// </summary>
internal sealed class PersistentConnection : IPersistentConnection
{
    private readonly IConnectionFactory _connFactory;
    private readonly AsyncLock _connLock = new();
    private readonly ILogger<PersistentConnection> _logger;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly uint _poolCount;
    private readonly AsyncLock _reconnectLock = new(); // 用于控制重连并发的信号量
    private readonly RabbitConfig config;
    private ChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;
    private bool _isReconnecting; // 用于控制重连尝试的标志位

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ILogger<PersistentConnection> logger, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
        config = options.Get(Constant.OptionName);
        _poolCount = config.PoolCount < 1 ? (uint)Environment.ProcessorCount : config.PoolCount;
        _logger = logger;
        _pipelineProvider = pipelineProvider;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            if (_connection is not null)
            {
                try
                {
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                    _connection.CallbackExceptionAsync -= OnCallbackException;
                    _connection.ConnectionBlockedAsync -= OnConnectionBlocked;
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogCritical("{Message}", ex.Message);
                }
            }
            if (_channelPool is null) return;
            try
            {
                _channelPool.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogCritical("{Message}", ex.Message);
            }
        }
        _disposed = true;
    }

    public bool IsConnected => _connection is not null && _connection.IsOpen;

    public async Task<IChannel> GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _channelPool is null
                    ? throw new("通道池为空")
                    : await _channelPool.GetChannel();

    public async Task ReturnChannel(IChannel channel)
    {
        if (_channelPool is null) throw new("通道池为空");
        await _channelPool.ReturnChannel(channel);
    }

    public async Task TryConnect()
    {
        _logger.LogInformation("RabbitMQ客户端尝试连接");
        using (await _connLock.LockAsync())
        {
            try
            {
                var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
                await pipeline.ExecuteAsync(async ct =>
                    _connection = config.AmqpTcpEndpoints is not null && config.AmqpTcpEndpoints.Count > 0
                                      ? await _connFactory.CreateConnectionAsync(config.AmqpTcpEndpoints, ct)
                                      : await _connFactory.CreateConnectionAsync(ct));
            }
            finally
            {
                // 先移除事件,以避免重复注册
                if (_connection is not null)
                {
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                    _connection.CallbackExceptionAsync -= OnCallbackException;
                    _connection.ConnectionBlockedAsync -= OnConnectionBlocked;
                }
                if (IsConnected && _connection is not null)
                {
                    _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                    _connection.CallbackExceptionAsync += OnCallbackException;
                    _connection.ConnectionBlockedAsync += OnConnectionBlocked;
                    _logger.LogInformation("RabbitMQ客户端与 {HostName} 建立了连接", _connection.Endpoint.HostName);
                    _channelPool = new(_connection, _poolCount);
                    _logger.LogInformation("RabbitBus channel pool count: {Count}", _poolCount);
                }
                else
                {
                    _logger.LogCritical("RabbitMQ连接不能被创建和打开");
                }
            }
        }
    }

    /// <summary>
    /// 重连
    /// </summary>
    /// <returns></returns>
    private async Task TryReconnect()
    {
        if (_isReconnecting) return;             // 如果已经在尝试重连,则直接返回
        using (await _reconnectLock.LockAsync()) // 请求重连锁
        {
            try
            {
                if (_isReconnecting) return; // 再次检查标志位,以避免重复重连
                _isReconnecting = true;      // 设置标志位,表示开始重连尝试
                _logger.LogWarning("RabbitMQ客户端尝试重新连接");
                await TryConnect(); // 尝试重新连接
            }
            finally
            {
                _isReconnecting = false; // 重置标志位,表示重连尝试结束
            }
        }
    }

    private Task OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        _logger.LogWarning("RabbitMQ连接被阻塞,原因: {Reason}", e.Reason);
        return TryReconnect();
    }

    private Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "RabbitMQ连接发生异常");
        return TryReconnect();
    }

    private Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        _logger.LogWarning("RabbitMQ连接关闭,原因: {Reason}", e.ReplyText);
        return TryReconnect();
    }
}