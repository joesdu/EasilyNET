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
/// <param name="poolCount">Channel线程数量</param>
/// <param name="endpoints"></param>
internal sealed class PersistentConnection(IConnectionFactory connFactory, ILogger<PersistentConnection> logger, int retry = 5, uint poolCount = 0, List<AmqpTcpEndpoint>? endpoints = null) : IPersistentConnection, IDisposable
{
    private readonly IConnectionFactory _connFactory = connFactory ?? throw new ArgumentNullException(nameof(connFactory));
    private readonly SemaphoreSlim _connLock = new(1, 1);
    private readonly uint _poolCount = poolCount < 1 ? (uint)Environment.ProcessorCount : poolCount;
    private ChannelPool? _channelPool;
    private IConnection? _connection;
    private bool _disposed;

    /// <inheritdoc />
    internal override bool IsConnected => _connection is not null && _connection.IsOpen;

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
    internal override async Task<IChannel> GetChannel() =>
        !IsConnected
            ? throw new InvalidOperationException("RabbitMQ连接失败")
            : _connection is null
                ? throw new InvalidOperationException("RabbitMQ连接未创建")
                : _channelPool is null
                    ? throw new("通道池为空")
                    : await _channelPool.GetChannel();

    /// <inheritdoc />
    internal override async Task ReturnChannel(IChannel channel) => await _channelPool!.ReturnChannel(channel);

    /// <inheritdoc />
    internal override async Task TryConnect()
    {
        logger.LogInformation("RabbitMQ客户端尝试连接");
        await _connLock.WaitAsync();
        try
        {
            var policy = Policy.Handle<SocketException>()
                               .Or<BrokerUnreachableException>()
                               .WaitAndRetry(retry, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                                   logger.LogWarning(ex, "RabbitMQ客户端在 {TimeOut}s 超时后无法创建链接,({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message));
            await policy.Execute(async () => _connection = endpoints is not null && endpoints.Count > 0 ? await _connFactory.CreateConnectionAsync(endpoints) : await _connFactory.CreateConnectionAsync());
        }
        finally
        {
            if (IsConnected && _connection is not null)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException;
                _connection.ConnectionBlocked += OnConnectionBlocked;
                logger.LogInformation("RabbitMQ客户端获取了与 {HostName} 的连接,并订阅了故障事件", _connection.Endpoint.HostName);
                _channelPool = new(_connection, _poolCount);
                logger.LogInformation("RabbitBus channel pool count: {Count}", _poolCount);
            }
            else
            {
                logger.LogCritical("RabbitMQ连接不能被创建和打开");
            }
            _connLock.Release();
        }
    }

    private void OnConnectionBlocked(object? sender, ConnectionBlockedEventArgs e)
    {
        logger.LogWarning("RabbitMQ连接关闭,正在尝试重新连接");
        Task.Factory.StartNew(async () => await TryConnect());
    }

    private void OnCallbackException(object? sender, CallbackExceptionEventArgs e)
    {
        logger.LogWarning("RabbitMQ连接抛出异常,正在重试");
        Task.Factory.StartNew(async () => await TryConnect());
    }

    private void OnConnectionShutdown(object? sender, ShutdownEventArgs reason)
    {
        logger.LogWarning("RabbitMQ连接处于关闭状态,正在尝试重新连接");
        Task.Factory.StartNew(async () => await TryConnect());
    }
}