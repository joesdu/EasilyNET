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
    ///     <para xml:lang="en">Sends a message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送消息。</para>
    /// </summary>
    Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, bool endOfMessage = true, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Sends a text message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送文本消息。</para>
    /// </summary>
    Task SendTextAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Sends a binary message to the client.</para>
    ///     <para xml:lang="zh">向客户端发送二进制消息。</para>
    /// </summary>
    Task SendBinaryAsync(byte[] bytes, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Closes the session.</para>
    ///     <para xml:lang="zh">关闭会话。</para>
    /// </summary>
    Task CloseAsync(WebSocketCloseStatus closeStatus, string? statusDescription, CancellationToken cancellationToken = default);
}