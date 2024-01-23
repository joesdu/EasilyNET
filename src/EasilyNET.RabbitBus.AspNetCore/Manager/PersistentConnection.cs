using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <summary>
/// RabbitMQ持久链接
/// </summary>
/// <param name="connFactory"></param>
/// <param name="logger"></param>
/// <param name="retry">重试次数</param>
/// <param name="maxChannel">Channel线程数量</param>
/// <param name="endpoints"></param>
internal sealed class PersistentConnection(IConnectionFactory connFactory, ILogger<PersistentConnection> logger, int retry = 5, uint maxChannel = 10, List<AmqpTcpEndpoint>? endpoints = null) : IPersistentConnection, IDisposable
{
    private readonly IConnectionFactory _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
    private readonly SemaphoreSlim _connLock = new(1, 1);
    private readonly uint _maxPoolCount = maxChannel < 1 ? (uint)Environment.ProcessorCount : maxChannel;
    private ChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;

    /// <inheritdoc />
    internal override bool IsConnected => _connection is not null && _connection.IsOpen && !_disposed;

    /// <inheritdoc />
    public void Dispose()
    {
        if (!_disposed)
        {
            if (_connection is not null)
            {
                try
                {
                    _connection.ConnectionShutdown -= OnConnectionShutdown;
                    _connection.CallbackException -= OnCallbackException;
                    _connection.ConnectionBlocked -= OnConnectionBlocked;
                    _connection.Dispose();
                }
                catch (Exception ex)
                {
                    logger.LogCritical("{Message}", ex.Message);
                }
            }
            if (_channelPool is null) return;
            try
            {
                _channelPool.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogCritical("{Message}", ex.Message);
            }
        }
        _disposed = true;
    }

    /// <inheritdoc />
    internal override IChannel GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _channelPool is null
                    ? throw new("通道池为空")
                    : _channelPool.GetChannel();

    /// <inheritdoc />
    internal override void ReturnChannel(IChannel channel) => _channelPool?.ReturnChannel(channel);

    /// <inheritdoc />
    internal override bool TryConnect()
    {
        logger.LogInformation("RabbitMQ客户端尝试连接");
        _connLock.Wait();
        try
        {
            var policy = Policy.Handle<SocketException>()
                               .Or<BrokerUnreachableException>()
                               .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   logger.LogWarning(ex, "RabbitMQ客户端在{TimeOut}s超时后无法创建链接,({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message));
            policy.Execute(() => _connection = endpoints is not null && endpoints.Count > 0 ? _connFactory.CreateConnection(endpoints) : _connFactory.CreateConnection());
            if (IsConnected && _connection is not null)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                logger.LogInformation("RabbitMQ客户端获取了与[{HostName}]的连接,并订阅了故障事件", _connection.Endpoint.HostName);
                _channelPool = new(_connection, _maxPoolCount);
                logger.LogInformation("RabbitBus channel pool max count: {Count}", _maxPoolCount);
                _disposed = false;
                return true;
            }
            else
            {
                logger.LogCritical("RabbitMQ连接不能被创建和打开");
                return false;
            }
        }
        finally
        {
            _connLock.Release();
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        logger.LogWarning("RabbitMQ连接关闭,正在尝试重新连接");
        TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        logger.LogWarning("RabbitMQ连接抛出异常,正在重试");
        TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        logger.LogWarning("RabbitMQ连接处于关闭状态,正在尝试重新连接");
        TryConnect();
    }
}