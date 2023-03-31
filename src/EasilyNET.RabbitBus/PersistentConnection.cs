using EasilyNET.RabbitBus.Abstraction;
using Microsoft.Extensions.Logging;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;

namespace EasilyNET.RabbitBus;

/// <summary>
/// RabbitMQ链接
/// </summary>
internal sealed class PersistentConnection : IPersistentConnection, IDisposable
{
    private readonly IConnectionFactory _connectionFactory;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly ILogger<PersistentConnection> _logger;
    private readonly int _retryCount;
    private IConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="connectionFactory"></param>
    /// <param name="logger"></param>
    /// <param name="retryCount"></param>
    /// <exception cref="ArgumentNullException"></exception>
    internal PersistentConnection(IConnectionFactory connectionFactory, ILogger<PersistentConnection> logger, int retryCount = 5)
    {
        _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _retryCount = retryCount;
    }

    /// <summary>
    /// 是否链接
    /// </summary>
    internal override bool IsConnected => _connection is not null && _connection.IsOpen && !_disposed;

    /// <summary>
    /// Dispose
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_connection is null) return;
        try
        {
            _connection.ConnectionShutdown -= OnConnectionShutdown;
            _connection.CallbackException -= OnCallbackException;
            _connection.ConnectionBlocked -= OnConnectionBlocked;
            _connection.Dispose();
        }
        catch (IOException ex)
        {
            _logger.LogCritical("{Message}", ex.Message);
        }
    }

    /// <summary>
    /// 创建Model
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    internal override IModel CreateModel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _connection.CreateModel();

    /// <summary>
    /// 尝试链接
    /// </summary>
    /// <returns></returns>
    internal override bool TryConnect()
    {
        _logger.LogInformation("RabbitMQ客户端尝试连接");
        _connectionLock.Wait();
        try
        {
            var policy = Policy.Handle<SocketException>().Or<BrokerUnreachableException>().WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (ex, time) => _logger.LogWarning(ex, "RabbitMQ客户端在{TimeOut}s超时后无法创建链接,({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message));
            _ = policy.Execute(() => _connection = _connectionFactory.CreateConnection());
            if (IsConnected && _connection is not null)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                _logger.LogInformation("RabbitMQ客户端获取了与[{HostName}]的持久连接,并订阅了故障事件", _connection.Endpoint.HostName);
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

    /// <summary>
    /// 当链接关闭
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接关闭,正在尝试重新连接...");
        _ = TryConnect();
    }

    /// <summary>
    /// 当链接抛出异常
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接抛出异常,在重试...");
        _ = TryConnect();
    }

    /// <summary>
    /// 当链接关闭
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="reason"></param>
    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        if (_disposed) return;
        _logger.LogWarning("RabbitMQ连接处于关闭状态,正在尝试重新连接...");
        _ = TryConnect();
    }
}