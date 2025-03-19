using EasilyNET.RabbitBus.AspNetCore.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.AspNetCore.Manager;

/// <inheritdoc />
internal sealed class PersistentConnection : IDisposable
{
    private readonly Lazy<IChannel> _channel;
    private readonly Lazy<IConnection> _connection;
    private readonly ILogger<PersistentConnection> _logger;
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

    public IChannel Channel => _channel.Value;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _connection.Value.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogCritical("Error disposing RabbitMQ connection: {Message}", ex.Message);
        }
        GC.SuppressFinalize(this);
    }

    ~PersistentConnection()
    {
        Dispose();
    }
}