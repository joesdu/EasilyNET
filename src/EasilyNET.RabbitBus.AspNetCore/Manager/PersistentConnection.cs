using EasilyNET.Core.Threading;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly.Registry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class PersistentConnection : IPersistentConnection
{
    private readonly RabbitConfig _config;
    private readonly IConnectionFactory _connFactory;
    private readonly AsyncLock _connLock = new();
    private readonly ILogger<PersistentConnection> _logger;
    private readonly ResiliencePipelineProvider<string> _pipelineProvider;
    private readonly uint _poolCount;
    private readonly AsyncLock _reconnectLock = new(); // 用于控制重连并发的信号量
    private ChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;
    private bool _isReconnecting; // 用于控制重连尝试的标志位

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ILogger<PersistentConnection> logger, ResiliencePipelineProvider<string> pipelineProvider)
    {
        _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
        _connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        _config = options.Get(Constant.OptionName);
        _poolCount = _config.PoolCount < 1 ? (uint)Environment.ProcessorCount : _config.PoolCount;
        _logger = logger;
        _pipelineProvider = pipelineProvider;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
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
                _logger.LogCritical("Error disposing RabbitMQ connection: {Message}", ex.Message);
            }
        }
        _channelPool?.Dispose();
    }

    public bool IsConnected => _connection is not null && _connection.IsOpen;

    public async Task<IChannel> GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ connection failed")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ connection not created")
                : _channelPool is null
                    ? throw new InvalidOperationException("Channel pool is null")
                    : await _channelPool.GetChannel();

    public async Task ReturnChannel(IChannel channel)
    {
        if (_channelPool is null) throw new InvalidOperationException("Channel pool is null");
        await _channelPool.ReturnChannel(channel);
    }

    public async Task TryConnect()
    {
        _logger.LogInformation("RabbitMQ client attempting to connect");
        using (await _connLock.LockAsync())
        {
            try
            {
                var pipeline = _pipelineProvider.GetPipeline(Constant.ResiliencePipelineName);
                await pipeline.ExecuteAsync(async ct => _connection = _config.AmqpTcpEndpoints is not null && _config.AmqpTcpEndpoints.Count > 0
                                                                          ? await _connFactory.CreateConnectionAsync(_config.AmqpTcpEndpoints, ct)
                                                                          : await _connFactory.CreateConnectionAsync(ct));
                if (_connection is not null)
                {
                    // Unsubscribe from events to avoid multiple subscriptions
                    _connection.ConnectionShutdownAsync -= OnConnectionShutdown;
                    _connection.CallbackExceptionAsync -= OnCallbackException;
                    _connection.ConnectionBlockedAsync -= OnConnectionBlocked;
                    if (IsConnected)
                    {
                        // Subscribe to events
                        _connection.ConnectionShutdownAsync += OnConnectionShutdown;
                        _connection.CallbackExceptionAsync += OnCallbackException;
                        _connection.ConnectionBlockedAsync += OnConnectionBlocked;
                        _logger.LogInformation("RabbitMQ client connected to {HostName}", _connection.Endpoint.HostName);
                        _channelPool = new(_connection, _poolCount);
                        _logger.LogInformation("RabbitBus channel pool count: {Count}", _poolCount);
                    }
                    else
                    {
                        _logger.LogCritical("RabbitMQ connection could not be created and opened");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("RabbitMQ connection attempt failed: {Message}", ex.Message);
            }
        }
    }

    /// <summary>
    /// Reconnect to RabbitMQ server.
    /// </summary>
    /// <returns></returns>
    private async Task TryReconnect()
    {
        if (_isReconnecting) return;             // If already trying to reconnect, return immediately
        using (await _reconnectLock.LockAsync()) // Request the reconnect lock
        {
            if (_isReconnecting) return; // Check the flag again to avoid duplicate reconnections
            _isReconnecting = true;      // Set the flag to indicate reconnection attempt
            var retryCount = 0;
            const int initialDelayMilliseconds = 1000;
            while (retryCount < _config.RetryCount && !IsConnected)
            {
                _logger.LogWarning("RabbitMQ client attempting to reconnect, attempt {Attempt}", retryCount + 1);
                await TryConnect();
                if (IsConnected)
                {
                    _logger.LogInformation("RabbitMQ client successfully reconnected on attempt {Attempt}", retryCount + 1);
                    break;
                }
                retryCount++;
                var delay = initialDelayMilliseconds * (int)Math.Pow(2, retryCount); // Exponential backoff
                _logger.LogWarning("RabbitMQ client reconnection attempt {Attempt} failed, retrying in {Delay} ms", retryCount, delay);
                await Task.Delay(delay);
            }
            if (!IsConnected)
            {
                _logger.LogCritical("RabbitMQ client failed to reconnect after {MaxAttempts} attempts", _config.RetryCount);
            }
            _isReconnecting = false; // Reset the flag to indicate reconnection attempt is over
        }
    }

    private async Task OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        try
        {
            _logger.LogWarning("RabbitMQ connection blocked, reason: {Reason}", e.Reason);
            await TryReconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling connection blocked event");
        }
    }

    private async Task OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        try
        {
            _logger.LogError(e.Exception, "RabbitMQ connection encountered an exception");
            await TryReconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling callback exception event");
        }
    }

    private async Task OnConnectionShutdown(object? sender, ShutdownEventArgs e)
    {
        try
        {
            _logger.LogWarning("RabbitMQ connection shutdown, reason: {Reason}", e.ReplyText);
            await TryReconnect();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while handling connection shutdown event");
        }
    }
}