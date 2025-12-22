using System.Net.WebSockets;
using System.Text;
using EasilyNET.Core.WebSocket;

namespace WebApi.Test.Unit.BackgroundServices;

internal sealed class WebSocketChatTestService(ILogger<WebSocketChatTestService> logger) : BackgroundService
{
    private readonly ManagedWebSocketClient _client = CreateClient(logger);

    private static ManagedWebSocketClient CreateClient(ILogger logger)
    {
        var options = new WebSocketClientOptions
        {
            ServerUri = new("ws://localhost:5046/ws/chat"),
            AutoReconnect = true,
            ReconnectDelayMs = 1000,
            HeartbeatEnabled = true,
            HeartbeatIntervalMs = 5000 // 测试时加快心跳频率
        };
        var client = new ManagedWebSocketClient(options);
        client.MessageReceived += (_, e) =>
        {
            if (e.MessageType == WebSocketMessageType.Text)
            {
                var message = Encoding.UTF8.GetString(e.Data.Span);
                logger.LogInformation("Client Received: {Message}", message);
            }
            else
            {
                logger.LogInformation("Client Received Binary: {Length} bytes", e.Data.Length);
            }
        };
        client.StateChanged += (_, e) => logger.LogInformation("Client State: {Previous} -> {Current}", e.PreviousState, e.CurrentState);
        client.Error += (_, e) => logger.LogError(e.Exception, "Client Error");
        client.Reconnecting += (_, e) => logger.LogWarning("Client Reconnecting: Attempt {Attempt}", e.AttemptNumber);
        return client;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Waiting for server to start...");
        // 延迟启动,避免服务端还未准备好就链接造成异常
        await Task.Delay(3000, stoppingToken);
        logger.LogInformation("Connecting...");
        await _client.ConnectAsync(stoppingToken);
        var counter = 0;

        // Test 1: Sequential Messaging
        logger.LogInformation("Starting Sequential Test...");
        for (var i = 0; i < 5; i++)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            var msg = $"Seq Message {Interlocked.Increment(ref counter)}";
            await _client.SendTextAsync(msg, stoppingToken);
            await Task.Delay(100, stoppingToken);
        }

        // Test 2: Concurrent Messaging (High Load)
        logger.LogInformation("Starting Concurrent Test...");
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            var msg = $"Concurrent Message {Interlocked.Increment(ref counter)}-{i}";
            return _client.SendTextAsync(msg, stoppingToken);
        });
        await Task.WhenAll(tasks);

        // Test 3: Binary Messaging
        logger.LogInformation("Starting Binary Test...");
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await _client.SendBinaryAsync(bytes, stoppingToken);

        // Test 4: Reconnection (Manual)
        logger.LogInformation("Testing Manual Reconnect...");
        await _client.DisconnectAsync();
        await Task.Delay(1000, stoppingToken);
        await _client.ConnectAsync(stoppingToken);
        await _client.SendTextAsync("I'm back!", stoppingToken);

        // Keep alive loop
        logger.LogInformation("Entering Keep-Alive Loop...");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_client.State == WebSocketClientState.Connected)
            {
                await _client.SendTextAsync($"Ping {DateTime.Now}", stoppingToken);
            }
            await Task.Delay(5000, stoppingToken);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_client.State != WebSocketClientState.Disposed)
        {
            await _client.DisconnectAsync();
            _client.Dispose();
        }
        await base.StopAsync(cancellationToken);
    }
}