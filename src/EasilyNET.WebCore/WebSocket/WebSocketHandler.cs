using EasilyNET.Core.WebSocket;

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Base class for handling WebSocket events.</para>
///     <para xml:lang="zh">处理 WebSocket 事件的基类。</para>
/// </summary>
public abstract class WebSocketHandler
{
    /// <summary>
    ///     <para xml:lang="en">Called when a new connection is established.</para>
    ///     <para xml:lang="zh">当建立新连接时调用。</para>
    /// </summary>
    public virtual Task OnConnectedAsync(IWebSocketSession session) => Task.CompletedTask;

    /// <summary>
    ///     <para xml:lang="en">Called when a connection is disconnected.</para>
    ///     <para xml:lang="zh">当连接断开时调用。</para>
    /// </summary>
    public virtual Task OnDisconnectedAsync(IWebSocketSession session) => Task.CompletedTask;

    /// <summary>
    ///     <para xml:lang="en">Called when a message is received.</para>
    ///     <para xml:lang="zh">当收到消息时调用。</para>
    /// </summary>
    public abstract Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message);

    /// <summary>
    ///     <para xml:lang="en">Called when an error occurs.</para>
    ///     <para xml:lang="zh">当发生错误时调用。</para>
    /// </summary>
    public virtual Task OnErrorAsync(IWebSocketSession session, Exception exception) => Task.CompletedTask;
}