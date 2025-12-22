// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Represents the connection state of a <see cref="ManagedWebSocketClient" />.</para>
///     <para xml:lang="zh">表示 <see cref="ManagedWebSocketClient" /> 的连接状态。</para>
/// </summary>
public enum WebSocketClientState
{
    /// <summary>
    ///     <para xml:lang="en">The client is disconnected and not attempting to connect.</para>
    ///     <para xml:lang="zh">客户端已断开连接且未尝试连接。</para>
    /// </summary>
    Disconnected = 0,

    /// <summary>
    ///     <para xml:lang="en">The client is attempting to establish a connection.</para>
    ///     <para xml:lang="zh">客户端正在尝试建立连接。</para>
    /// </summary>
    Connecting = 1,

    /// <summary>
    ///     <para xml:lang="en">The client is connected and ready to send/receive messages.</para>
    ///     <para xml:lang="zh">客户端已连接并准备好发送/接收消息。</para>
    /// </summary>
    Connected = 2,

    /// <summary>
    ///     <para xml:lang="en">The client is attempting to reconnect after a connection loss.</para>
    ///     <para xml:lang="zh">客户端在连接丢失后正在尝试重新连接。</para>
    /// </summary>
    Reconnecting = 3,

    /// <summary>
    ///     <para xml:lang="en">The client is gracefully closing the connection.</para>
    ///     <para xml:lang="zh">客户端正在优雅地关闭连接。</para>
    /// </summary>
    Closing = 4,

    /// <summary>
    ///     <para xml:lang="en">The client has been disposed and cannot be used.</para>
    ///     <para xml:lang="zh">客户端已被释放且无法使用。</para>
    /// </summary>
    Disposed = 5
}