using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class PersistentConnection : IDisposable
{
    private readonly Lazy<IConnection> _connection;

    // 为了线程安全，可以使用信号量来保护通道获取
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ILogger<PersistentConnection> _logger;
    private Lazy<IChannel> _channel;
    private bool _disposed;

    public PersistentConnection(IConnectionFactory connFactory, IOptionsMonitor<RabbitConfig> options, ILogger<PersistentConnection> logger)
    {
        connFactory.RequestedHeartbeat = TimeSpan.FromSeconds(30);
        var _config = options.Get(Constant.OptionName);
        _logger = logger;
        _connection = new(() => _config.AmqpTcpEndpoints is not null && _config.AmqpTcpEndpoints.Count > 0
                                    ? connFactory.CreateConnectionAsync(_config.AmqpTcpEndpoints).Result
                                    : connFactory.CreateConnectionAsync().Result);
        _channel = new(() => _connection.Value.CreateChannelAsync().Result);
    }

    /// <summary>
    /// 连接状态
    /// </summary>
    public bool IsConnected => _connection is { IsValueCreated: true, Value.IsOpen: true } && !_disposed;

    public IChannel Channel => _channel.Value;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        try
        {
            _connection.Value.Dispose();
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Critical))
            {
                _logger.LogCritical("Error disposing RabbitMQ connection: {Message}", ex.Message);
            }
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 重连
    /// </summary>
    /// <returns></returns>
    public async Task TryConnect()
    {
        if (IsConnected)
        {
            return;
        }
        // 如果已经被释放，不允许重连
        if (_disposed)
        {
            return;
        }
        try
        {
            // 重新初始化连接和通道
            _channel = new(() => _connection.Value.CreateChannelAsync().Result);
        }
        catch (Exception ex)
        {
            if (_logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(ex, "无法建立与RabbitMQ的连接");
            }
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取通道的安全方法
    /// </summary>
    /// <returns></returns>
    public async Task<IChannel> GetChannelAsync()
    {
        await _lock.WaitAsync();
        try
        {
            if (!IsConnected)
            {
                await TryConnect();
            }
            return Channel;
        }
        finally
        {
            _lock.Release();
        }
    }

    ~PersistentConnection()
    {
        Dispose();
    }
}