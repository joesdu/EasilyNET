using EasilyNET.RabbitBus.Abstraction;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EasilyNET.RabbitBus.Manager;

/// <summary>
/// RabbitMQ持久链接
/// </summary>
internal sealed class PersistentConnection : IPersistentConnection, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger<PersistentConnection> _logger;
    private readonly uint _maxPoolCount;
    private readonly int _retryCount;
    private readonly List<AmqpTcpEndpoint>? _tcpEndpoints;
    private IChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionFactory"></param>
    /// <param name="logger"></param>
    /// <param name="maxChannelCount">Channel线程数量</param>
    /// <param name="tcpEndpoints"></param>
    /// <param name="retryCount"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal PersistentConnection(IConnectionFactory connectionFactory, ILogger<PersistentConnection> logger, int retryCount = 5, uint maxChannelCount = 10, List<AmqpTcpEndpoint>? tcpEndpoints = null)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
        _tcpEndpoints = tcpEndpoints;
        _maxPoolCount = maxChannelCount < 1 ? (uint)Environment.ProcessorCount : maxChannelCount;
    }

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

    /// <inheritdoc />
    internal override IModel GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _channelPool is null
                    ? throw new("通道池为空")
                    : _channelPool.GetChannel();

    /// <inheritdoc />
    internal override void ReturnChannel(IModel channel) => _channelPool?.ReturnChannel(channel);

    /// <inheritdoc />
    internal override bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ客户端尝试连接");
        _connectionLock.Wait();
        try
        {
            var policy = Policy.Handle<SocketException>()
                               .Or<BrokerUnreachableException>()
                               .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   _logger.LogWarning(ex, "RabbitMQ客户端在{TimeOut}s超时后无法创建链接,({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message));
            policy.Execute(() => _connection = _tcpEndpoints is not null && _tcpEndpoints.Count > 0 ? _connectionFactory.CreateConnection(_tcpEndpoints) : _connectionFactory.CreateConnection());
            if (IsConnected && _connection is not null)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                _logger.LogInformation("RabbitMQ客户端获取了与[{HostName}]的持久连接,并订阅了故障事件", _connection.Endpoint.HostName);
                _channelPool = new ChannelPool(_connection, _maxPoolCount);
                _disposed = false;
                return true;
            }
            else
            {
                _logger.LogCritical("RabbitMQ连接不能被创建和打开");
                return false;
            }
        }
        finally
        {
            _ = _connectionLock.Release();
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接关闭,正在尝试重新连接...");
        _ = TryConnect();
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接抛出异常,在重试...");
        _ = TryConnect();
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接处于关闭状态,正在尝试重新连接...");
        _ = TryConnect();
    }
}