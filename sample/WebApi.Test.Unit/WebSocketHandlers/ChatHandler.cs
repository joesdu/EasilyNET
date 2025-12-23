using System.Net.WebSockets;
using System.Text;
using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.WebSocket;
using EasilyNET.WebCore.WebSocket;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.WebSocketHandlers;

/// <summary>
/// 写一个简单的聊天处理器来测试 WebSocket 连接
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton)]
public sealed class ChatHandler(ILogger<ChatHandler> logger) : WebSocketHandler
{
    /// <inheritdoc />
    public override async Task OnConnectedAsync(IWebSocketSession session)
    {
        logger.LogInformation("Client connected: {SessionId}", session.Id);
        await session.SendTextAsync("Welcome to the chat!");
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(IWebSocketSession session)
    {
        logger.LogInformation("Client disconnected: {SessionId}", session.Id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(message.Data.Span);
            logger.LogInformation("Received: {Text}", text);

            // Echo back
            await session.SendTextAsync($"Echo: {text}");
        }
    }
}