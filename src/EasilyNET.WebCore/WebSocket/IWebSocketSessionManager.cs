using System.Net.WebSockets;

// ReSharper disable UnusedMemberInSuper.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Manages WebSocket sessions, providing session tracking and broadcast capabilities.</para>
///     <para xml:lang="zh">管理 WebSocket 会话，提供会话跟踪和广播功能。</para>
/// </summary>
public interface IWebSocketSessionManager
{
    /// <summary>
    ///     <para xml:lang="en">Gets the number of active sessions.</para>
    ///     <para xml:lang="zh">获取活动会话的数量。</para>
    /// </summary>
    int Count { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets all active sessions.</para>
    ///     <para xml:lang="zh">获取所有活动会话。</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A read-only collection of all active sessions.</para>
    ///     <para xml:lang="zh">所有活动会话的只读集合。</para>
    /// </returns>
    IReadOnlyCollection<IWebSocketSession> GetAllSessions();

    /// <summary>
    ///     <para xml:lang="en">Gets a session by its identifier.</para>
    ///     <para xml:lang="zh">根据标识符获取会话。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The session identifier.</para>
    ///     <para xml:lang="zh">会话标识符。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The session if found; otherwise, null.</para>
    ///     <para xml:lang="zh">如果找到则返回会话；否则返回 null。</para>
    /// </returns>
    IWebSocketSession? GetSession(string id);

    /// <summary>
    ///     <para xml:lang="en">Broadcasts a message to all active sessions.</para>
    ///     <para xml:lang="zh">向所有活动会话广播消息。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">The message data to broadcast.</para>
    ///     <para xml:lang="zh">要广播的消息数据。</para>
    /// </param>
    /// <param name="messageType">
    ///     <para xml:lang="en">The type of WebSocket message.</para>
    ///     <para xml:lang="zh">WebSocket 消息类型。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task representing the asynchronous broadcast operation.</para>
    ///     <para xml:lang="zh">表示异步广播操作的任务。</para>
    /// </returns>
    Task BroadcastAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Broadcasts a text message to all active sessions.</para>
    ///     <para xml:lang="zh">向所有活动会话广播文本消息。</para>
    /// </summary>
    /// <param name="text">
    ///     <para xml:lang="en">The text to broadcast.</para>
    ///     <para xml:lang="zh">要广播的文本。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task representing the asynchronous broadcast operation.</para>
    ///     <para xml:lang="zh">表示异步广播操作的任务。</para>
    /// </returns>
    Task BroadcastTextAsync(string text, CancellationToken cancellationToken = default);
}