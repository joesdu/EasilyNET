using System.Net.WebSockets;
using System.Text;
using EasilyNET.Core.WebSocket;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace WebApi.Test.Unit.BackgroundServices;

internal sealed class WebSocketChatTestService(ILogger<WebSocketChatTestService> logger, IServer server, IHostApplicationLifetime appLifetime) : BackgroundService
{
    private ManagedWebSocketClient? _client;

    private static ManagedWebSocketClient CreateClient(ILogger logger, Uri serverUri)
    {
        var options = new WebSocketClientOptions
        {
            ServerUri = serverUri,
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
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Client Received: {Message}", message);
                }
            }
            else
            {
                if (logger.IsEnabled(LogLevel.Information))
                {
                    logger.LogInformation("Client Received Binary: {Length} bytes", e.Data.Length);
                }
            }
        };
        client.StateChanged += (_, e) =>
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Client State: {Previous} -> {Current}", e.PreviousState, e.CurrentState);
            }
        };
        client.Error += (_, e) =>
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(e.Exception, "Client Error");
            }
        };
        client.Reconnecting += (_, e) =>
        {
            if (logger.IsEnabled(LogLevel.Warning))
            {
                logger.LogWarning("Client Reconnecting: Attempt {Attempt}", e.AttemptNumber);
            }
        };
        return client;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, appLifetime.ApplicationStopping);
        var token = cts.Token;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Waiting for server to start...");
        }
        // 延迟启动,避免服务端还未准备好就连接造成异常
        await Task.Delay(3000, token);
        var addresses = server.Features.Get<IServerAddressesFeature>()?.Addresses;
        if (addresses is null || addresses.Count == 0)
        {
            logger.LogError("No server addresses found.");
            return;
        }
        var address = addresses.First();
        // 处理通配符地址
        address = address.Replace("://*", "://localhost").Replace("://+", "://localhost");
        var uriBuilder = new UriBuilder(address);
        uriBuilder.Scheme = uriBuilder.Scheme == "https" ? "wss" : "ws";
        uriBuilder.Path = "ws/chat";
        var serverUri = uriBuilder.Uri;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Connecting to {Uri}...", serverUri);
        }
        _client = CreateClient(logger, serverUri);
        await _client.ConnectAsync(token);
        var counter = 0;

        // Test 1: Sequential Messaging
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting Sequential Test...");
        }
        for (var i = 0; i < 5; i++)
        {
            if (token.IsCancellationRequested)
            {
                break;
            }
            var msg = $"Seq Message {Interlocked.Increment(ref counter)}";
            await _client.SendTextAsync(msg, token);
            await Task.Delay(100, token);
        }

        // Test 2: Concurrent Messaging (High Load)
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting Concurrent Test...");
        }
        var tasks = Enumerable.Range(0, 10).Select(i =>
        {
            var msg = $"Concurrent Message {Interlocked.Increment(ref counter)}-{i}";
            return _client.SendTextAsync(msg, token);
        });
        await Task.WhenAll(tasks);

        // Test 3: Binary Messaging
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Starting Binary Test...");
        }
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        await _client.SendBinaryAsync(bytes, token);

        // Test 4: Reconnection (Manual)
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Testing Manual Reconnect...");
        }
        await _client.DisconnectAsync();
        await Task.Delay(1000, token);
        await _client.ConnectAsync(token);
        await _client.SendTextAsync("I'm back!", token);

        // Keep alive loop
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Entering Keep-Alive Loop...");
        }
        while (!token.IsCancellationRequested)
        {
            if (_client.State == WebSocketClientState.Connected)
            {
                await _client.SendTextAsync($"Ping {DateTime.Now}", token);
            }
            await Task.Delay(5000, token);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("WebSocketChatTestService is stopping...");
        }
        await base.StopAsync(cancellationToken);
        try
        {
            if (_client is not null)
            {
                await _client.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error disposing client during stop");
        }
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("WebSocketChatTestService stopped.");
        }
    }
}