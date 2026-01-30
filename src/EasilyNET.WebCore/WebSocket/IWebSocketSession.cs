using System.Net.WebSockets;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedMemberInSuper.Global

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Represents a WebSocket session on the server.</para>
///     <para xml:lang="zh">表示服务器上的 WebSocket 会话。</para>
/// </summary>
public interface IWebSocketSession
{
    /// <summary>
    ///     <para xml:lang="en">Gets the unique identifier of the session.</para>
    ///     <para xml:lang="zh">获取会话的唯一标识符。</para>
    /// </summary>
    string Id { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the current state of the WebSocket connection.</para>
    ///     <para xml:lang="zh">获取 WebSocket 连接的当前状态。</para>
    /// </summary>
    WebSocketState State { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a key/value collection that can be used to share data within the scope of this session.</para>
    ///     <para xml:lang="zh">获取可用于在此会话范围内共享数据的键/值集合。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     This property provides a way to store custom data associated with the session.
    ///     The collection is thread-safe and can be used to store user information, authentication data, or any other session-specific state.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此属性提供了一种存储与会话关联的自定义数据的方式。
    ///     该集合是线程安全的，可用于存储用户信息、身份验证数据或任何其他会话特定状态。
    ///     </para>
    /// </remarks>
    IDictionary<string, object?> Items { get; }

    /// <summary>
    ///     <para xml:lang="en">Sends a message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送消息。</para>
    /// </summary>
    /// <param name="message">
    ///     <para xml:lang="en">The message data to send.</para>
    ///     <para xml:lang="zh">要发送的消息数据。</para>
    /// </param>
    /// <param name="messageType">
    ///     <para xml:lang="en">The type of WebSocket message.</para>
    ///     <para xml:lang="zh">WebSocket 消息类型。</para>
    /// </param>
    /// <param name="endOfMessage">
    ///     <para xml:lang="en">Whether this is the end of the message.</para>
    ///     <para xml:lang="zh">是否为消息结尾。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Sends a text message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送文本消息。</para>
    /// </summary>
    /// <param name="text">
    ///     <para xml:lang="en">The text to send.</para>
    ///     <para xml:lang="zh">要发送的文本。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    Task SendTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送二进制消息。</para>
    /// </summary>
    /// <param name="bytes">
    ///     <para xml:lang="en">The binary data to send.</para>
    ///     <para xml:lang="zh">要发送的二进制数据。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message to the client from a ReadOnlyMemory.</para>
    ///     <para xml:lang="zh">从 ReadOnlyMemory 向客户端发送二进制消息。</para>
    /// </summary>
    /// <param name="data">
    ///     <para xml:lang="en">The binary data to send.</para>
    ///     <para xml:lang="zh">要发送的二进制数据。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    Task SendBinaryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Closes the session.</para>
    ///     <para xml:lang="zh">关闭会话。</para>
    /// </summary>
    /// <param name="closeStatus">
    ///     <para xml:lang="en">The close status code.</para>
    ///     <para xml:lang="zh">关闭状态码。</para>
    /// </param>
    /// <param name="statusDescription">
    ///     <para xml:lang="en">The close status description.</para>
    ///     <para xml:lang="zh">关闭状态描述。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">The cancellation token.</para>
    ///     <para xml:lang="zh">取消令牌。</para>
    /// </param>
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default);
}