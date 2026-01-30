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
public sealed class WebSocketSessionManager : IWebSocketSessionManager
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

    /// <summary>
    ///     <para xml:lang="en">Adds a session to the manager. For internal use by middleware.</para>
    ///     <para xml:lang="zh">将会话添加到管理器。供中间件内部使用。</para>
    /// </summary>
    /// <param name="session">
    ///     <para xml:lang="en">The session to add.</para>
    ///     <para xml:lang="zh">要添加的会话。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the session was added; false if a session with the same ID already exists.</para>
    ///     <para xml:lang="zh">如果会话已添加则返回 true；如果已存在相同 ID 的会话则返回 false。</para>
    /// </returns>
    internal bool AddSession(IWebSocketSession session) => _sessions.TryAdd(session.Id, session);

    /// <summary>
    ///     <para xml:lang="en">Removes a session from the manager. For internal use by middleware.</para>
    ///     <para xml:lang="zh">从管理器中移除会话。供中间件内部使用。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the session to remove.</para>
    ///     <para xml:lang="zh">要移除的会话的 ID。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the session was removed; false if no session with the given ID was found.</para>
    ///     <para xml:lang="zh">如果会话已移除则返回 true；如果未找到给定 ID 的会话则返回 false。</para>
    /// </returns>
    internal bool RemoveSession(string id) => _sessions.TryRemove(id, out _);
}