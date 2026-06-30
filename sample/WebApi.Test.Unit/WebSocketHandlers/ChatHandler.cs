using EasilyNET.AutoDependencyInjection.Core.Attributes;
using EasilyNET.Core.WebSocket;
using EasilyNET.WebCore.WebSocket;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;

// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.WebSocketHandlers;

/// <summary>
/// 一个简单的聊天处理器，用于演示并测试服务端 WebSocket：
/// 连接时发送 "Welcome to the chat!"，文本消息回显为 "Echo: {text}"，二进制消息仅记录长度（不回显）。
/// 同时演示 <see cref="IWebSocketSession.Items" /> 的按连接状态存储（连接时间、消息计数）以及 <see cref="OnErrorAsync" /> 的用法。
/// </summary>
[DependencyInjection(ServiceLifetime.Singleton)]
public sealed class ChatHandler(ILogger<ChatHandler> logger) : WebSocketHandler
{
    private const string StartTimestampKey = "StartTimestamp";
    private const string MessageCountKey = "MessageCount";

    /// <summary>日志中单条文本预览的最大字符数，避免把大消息（如几百 KB）整段打进日志。</summary>
    private const int PreviewLength = 64;

    /// <inheritdoc />
    public override async Task OnConnectedAsync(IWebSocketSession session)
    {
        // 按连接的数据存入 Items（Handler 是单例、被所有连接共享，连接级状态不能放字段）
        // 用单调时钟时间戳测量在线时长，免疫系统时钟跳变（NTP 校时 / 夏令时 / 手动改时间）
        session.Items[StartTimestampKey] = Stopwatch.GetTimestamp();
        session.Items[MessageCountKey] = 0;
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Client connected: {SessionId}", session.Id);
        }
        await session.SendTextAsync("Welcome to the chat!");
    }

    /// <inheritdoc />
    public override Task OnDisconnectedAsync(IWebSocketSession session)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            var count = session.Items.TryGetValue(MessageCountKey, out var c) && c is int ci ? ci : 0;
            var duration = session.Items.TryGetValue(StartTimestampKey, out var t) && t is long startTimestamp
                               ? Stopwatch.GetElapsedTime(startTimestamp)
                               : TimeSpan.Zero;
            logger.LogInformation("Client disconnected: {SessionId} (处理消息 {Count} 条, 在线 {Milliseconds:N1}ms)", session.Id, count, duration.TotalMilliseconds);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        IncrementMessageCount(session);
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(message.Data.Span);
            if (logger.IsEnabled(LogLevel.Information))
            {
                // 只记录预览 + 总长度，避免大消息刷屏 / 拖慢日志
                logger.LogInformation("Received text from {SessionId}: {Preview} ({Length} chars)", session.Id, Preview(text), text.Length);
            }
            // 回显文本
            await session.SendTextAsync($"Echo: {text}");
            return;
        }
        // 二进制消息：仅记录长度，不回显（与客户端测试约定一致）
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Received binary from {SessionId}: {Length} bytes", session.Id, message.Data.Length);
        }
    }

    /// <inheritdoc />
    public override Task OnErrorAsync(IWebSocketSession session, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(exception, "Session {SessionId} error", session.Id);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 递增当前连接的消息计数。同一连接的 <see cref="OnMessageAsync" /> 串行调用，无需额外同步。
    /// </summary>
    private static void IncrementMessageCount(IWebSocketSession session)
    {
        var current = session.Items.TryGetValue(MessageCountKey, out var c) && c is int ci ? ci : 0;
        session.Items[MessageCountKey] = current + 1;
    }

    /// <summary>截取文本预览，超长则附省略号。</summary>
    private static string Preview(string text) => text.Length <= PreviewLength ? text : $"{text[..PreviewLength]}…";
}
