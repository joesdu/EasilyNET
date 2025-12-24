using System.Net.WebSockets;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket state changes.</para>
///     <para xml:lang="zh">WebSocket 状态变更事件参数。</para>
/// </summary>
/// <param name="previousState">
///     <para xml:lang="en">The previous state.</para>
///     <para xml:lang="zh">先前的状态。</para>
/// </param>
/// <param name="currentState">
///     <para xml:lang="en">The current state.</para>
///     <para xml:lang="zh">当前状态。</para>
/// </param>
public sealed class WebSocketStateChangedEventArgs(WebSocketClientState previousState, WebSocketClientState currentState) : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Gets the previous state before the change.</para>
    ///     <para xml:lang="zh">获取变更前的状态。</para>
    /// </summary>
    public WebSocketClientState PreviousState { get; } = previousState;

    /// <summary>
    ///     <para xml:lang="en">Gets the current state after the change.</para>
    ///     <para xml:lang="zh">获取变更后的当前状态。</para>
    /// </summary>
    public WebSocketClientState CurrentState { get; } = currentState;
}

/// <summary>
///     <para xml:lang="en">Event arguments for received WebSocket messages.</para>
///     <para xml:lang="zh">接收到的 WebSocket 消息事件参数。</para>
/// </summary>
/// <param name="data">
///     <para xml:lang="en">The message data.</para>
///     <para xml:lang="zh">消息数据。</para>
/// </param>
/// <param name="messageType">
///     <para xml:lang="en">The type of the message.</para>
///     <para xml:lang="zh">消息类型。</para>
/// </param>
/// <param name="endOfMessage">
///     <para xml:lang="en">Whether this is the end of the message.</para>
///     <para xml:lang="zh">是否为消息结尾。</para>
/// </param>
public sealed class WebSocketMessageReceivedEventArgs(ReadOnlyMemory<byte> data, WebSocketMessageType messageType, bool endOfMessage) : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Gets the received message data.</para>
    ///     <para xml:lang="zh">获取接收到的消息数据。</para>
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; } = data;

    /// <summary>
    ///     <para xml:lang="en">Gets the type of the WebSocket message.</para>
    ///     <para xml:lang="zh">获取 WebSocket 消息类型。</para>
    /// </summary>
    public WebSocketMessageType MessageType { get; } = messageType;

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether this is the end of the message.</para>
    ///     <para xml:lang="zh">获取一个值，指示这是否是消息的结尾。</para>
    /// </summary>
    public bool EndOfMessage { get; } = endOfMessage;
}

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket errors.</para>
///     <para xml:lang="zh">WebSocket 错误事件参数。</para>
/// </summary>
/// <param name="exception">
///     <para xml:lang="en">The exception that occurred.</para>
///     <para xml:lang="zh">发生的异常。</para>
/// </param>
/// <param name="context">
///     <para xml:lang="en">The context in which the error occurred.</para>
///     <para xml:lang="zh">发生错误的上下文。</para>
/// </param>
public sealed class WebSocketErrorEventArgs(Exception exception, string context) : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Gets the exception that caused the error.</para>
    ///     <para xml:lang="zh">获取导致错误的异常。</para>
    /// </summary>
    public Exception Exception { get; } = exception;

    /// <summary>
    ///     <para xml:lang="en">Gets the context describing where the error occurred.</para>
    ///     <para xml:lang="zh">获取描述错误发生位置的上下文。</para>
    /// </summary>
    public string Context { get; } = context;
}

/// <summary>
///     <para xml:lang="en">Event arguments for reconnection attempts.</para>
///     <para xml:lang="zh">重连尝试事件参数。</para>
/// </summary>
/// <param name="attemptNumber">
///     <para xml:lang="en">The current attempt number.</para>
///     <para xml:lang="zh">当前尝试次数。</para>
/// </param>
/// <param name="delay">
///     <para xml:lang="en">The delay before the next attempt.</para>
///     <para xml:lang="zh">下次尝试前的延迟。</para>
/// </param>
/// <param name="lastException">
///     <para xml:lang="en">The exception from the last failed attempt.</para>
///     <para xml:lang="zh">上次失败尝试的异常。</para>
/// </param>
public sealed class WebSocketReconnectingEventArgs(int attemptNumber, TimeSpan delay, Exception? lastException) : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Gets the current reconnection attempt number.</para>
    ///     <para xml:lang="zh">获取当前重连尝试次数。</para>
    /// </summary>
    public int AttemptNumber { get; } = attemptNumber;

    /// <summary>
    ///     <para xml:lang="en">Gets the delay before the next reconnection attempt.</para>
    ///     <para xml:lang="zh">获取下次重连尝试前的延迟。</para>
    /// </summary>
    public TimeSpan Delay { get; } = delay;

    /// <summary>
    ///     <para xml:lang="en">Gets the exception from the last failed attempt, if any.</para>
    ///     <para xml:lang="zh">获取上次失败尝试的异常（如果有）。</para>
    /// </summary>
    public Exception? LastException { get; } = lastException;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets a value indicating whether to cancel the reconnection attempt.</para>
    ///     <para xml:lang="zh">获取或设置一个值，指示是否取消重连尝试。</para>
    /// </summary>
    public bool Cancel { get; set; }
}

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket close events.</para>
///     <para xml:lang="zh">WebSocket 关闭事件参数。</para>
/// </summary>
/// <param name="closeStatus">
///     <para xml:lang="en">The close status code.</para>
///     <para xml:lang="zh">关闭状态码。</para>
/// </param>
/// <param name="closeDescription">
///     <para xml:lang="en">The close description.</para>
///     <para xml:lang="zh">关闭描述。</para>
/// </param>
/// <param name="initiatedByClient">
///     <para xml:lang="en">Whether the close was initiated by the client.</para>
///     <para xml:lang="zh">是否由客户端发起关闭。</para>
/// </param>
public sealed class WebSocketClosedEventArgs(WebSocketCloseStatus? closeStatus, string? closeDescription, bool initiatedByClient) : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Gets the WebSocket close status code.</para>
    ///     <para xml:lang="zh">获取 WebSocket 关闭状态码。</para>
    /// </summary>
    public WebSocketCloseStatus? CloseStatus { get; } = closeStatus;

    /// <summary>
    ///     <para xml:lang="en">Gets the close status description.</para>
    ///     <para xml:lang="zh">获取关闭状态描述。</para>
    /// </summary>
    public string? CloseDescription { get; } = closeDescription;

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the close was initiated by the client.</para>
    ///     <para xml:lang="zh">获取一个值，指示是否由客户端发起关闭。</para>
    /// </summary>
    public bool InitiatedByClient { get; } = initiatedByClient;
}