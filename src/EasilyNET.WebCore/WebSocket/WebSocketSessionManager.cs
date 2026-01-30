using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

// ReSharper disable UnusedMethodReturnValue.Global

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Default implementation of <see cref="IWebSocketSessionManager" />.</para>
///     <para xml:lang="zh"><see cref="IWebSocketSessionManager" /> 的默认实现。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     This class provides thread-safe session tracking and broadcast capabilities for WebSocket connections.
///     It is designed to be registered as a singleton in the dependency injection container.
///     </para>
///     <para xml:lang="zh">
///     此类为 WebSocket 连接提供线程安全的会话跟踪和广播功能。
///     它被设计为在依赖注入容器中注册为单例。
///     </para>
/// </remarks>
public sealed class WebSocketSessionManager : IWebSocketSessionManager, IWebSocketSessionRegistry
{
    private readonly ConcurrentDictionary<string, IWebSocketSession> _sessions = new();

    /// <inheritdoc />
    public int Count => _sessions.Count;

    /// <inheritdoc />
    public IReadOnlyCollection<IWebSocketSession> GetAllSessions() => _sessions.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public IWebSocketSession? GetSession(string id) => _sessions.GetValueOrDefault(id);

    /// <inheritdoc />
    public async Task BroadcastAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, CancellationToken cancellationToken = default)
    {
        var tasks = _sessions.Values
                             .Where(s => s.State == WebSocketState.Open)
                             .Select(s => s.SendAsync(message, messageType, true, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task BroadcastTextAsync(string text, CancellationToken cancellationToken = default)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return BroadcastAsync(bytes, WebSocketMessageType.Text, cancellationToken);
    }

    /// <inheritdoc />
    bool IWebSocketSessionRegistry.AddSession(IWebSocketSession session) => _sessions.TryAdd(session.Id, session);

    /// <inheritdoc />
    bool IWebSocketSessionRegistry.RemoveSession(string id) => _sessions.TryRemove(id, out _);
}