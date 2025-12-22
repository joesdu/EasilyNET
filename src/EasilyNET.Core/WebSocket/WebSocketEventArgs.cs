using System.Net.WebSockets;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket state changes.</para>
///     <para xml:lang="zh">WebSocket 状态变更事件参数。</para>
/// </summary>
public sealed class WebSocketStateChangedEventArgs : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketStateChangedEventArgs" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketStateChangedEventArgs" /> 类的新实例。</para>
    /// </summary>
    /// <param name="previousState">
    ///     <para xml:lang="en">The previous state.</para>
    ///     <para xml:lang="zh">先前的状态。</para>
    /// </param>
    /// <param name="currentState">
    ///     <para xml:lang="en">The current state.</para>
    ///     <para xml:lang="zh">当前状态。</para>
    /// </param>
    public WebSocketStateChangedEventArgs(WebSocketClientState previousState, WebSocketClientState currentState)
    {
        PreviousState = previousState;
        CurrentState = currentState;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the previous state before the change.</para>
    ///     <para xml:lang="zh">获取变更前的状态。</para>
    /// </summary>
    public WebSocketClientState PreviousState { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the current state after the change.</para>
    ///     <para xml:lang="zh">获取变更后的当前状态。</para>
    /// </summary>
    public WebSocketClientState CurrentState { get; }
}

/// <summary>
///     <para xml:lang="en">Event arguments for received WebSocket messages.</para>
///     <para xml:lang="zh">接收到的 WebSocket 消息事件参数。</para>
/// </summary>
public sealed class WebSocketMessageReceivedEventArgs : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketMessageReceivedEventArgs" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketMessageReceivedEventArgs" /> 类的新实例。</para>
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
    public WebSocketMessageReceivedEventArgs(ReadOnlyMemory<byte> data, WebSocketMessageType messageType, bool endOfMessage)
    {
        Data = data;
        MessageType = messageType;
        EndOfMessage = endOfMessage;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the received message data.</para>
    ///     <para xml:lang="zh">获取接收到的消息数据。</para>
    /// </summary>
    public ReadOnlyMemory<byte> Data { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the type of the WebSocket message.</para>
    ///     <para xml:lang="zh">获取 WebSocket 消息类型。</para>
    /// </summary>
    public WebSocketMessageType MessageType { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether this is the end of the message.</para>
    ///     <para xml:lang="zh">获取一个值，指示这是否是消息的结尾。</para>
    /// </summary>
    public bool EndOfMessage { get; }
}

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket errors.</para>
///     <para xml:lang="zh">WebSocket 错误事件参数。</para>
/// </summary>
public sealed class WebSocketErrorEventArgs : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketErrorEventArgs" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketErrorEventArgs" /> 类的新实例。</para>
    /// </summary>
    /// <param name="exception">
    ///     <para xml:lang="en">The exception that occurred.</para>
    ///     <para xml:lang="zh">发生的异常。</para>
    /// </param>
    /// <param name="context">
    ///     <para xml:lang="en">The context in which the error occurred.</para>
    ///     <para xml:lang="zh">发生错误的上下文。</para>
    /// </param>
    public WebSocketErrorEventArgs(Exception exception, string context)
    {
        Exception = exception;
        Context = context;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the exception that caused the error.</para>
    ///     <para xml:lang="zh">获取导致错误的异常。</para>
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the context describing where the error occurred.</para>
    ///     <para xml:lang="zh">获取描述错误发生位置的上下文。</para>
    /// </summary>
    public string Context { get; }
}

/// <summary>
///     <para xml:lang="en">Event arguments for reconnection attempts.</para>
///     <para xml:lang="zh">重连尝试事件参数。</para>
/// </summary>
public sealed class WebSocketReconnectingEventArgs : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketReconnectingEventArgs" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketReconnectingEventArgs" /> 类的新实例。</para>
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
    public WebSocketReconnectingEventArgs(int attemptNumber, TimeSpan delay, Exception? lastException)
    {
        AttemptNumber = attemptNumber;
        Delay = delay;
        LastException = lastException;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the current reconnection attempt number.</para>
    ///     <para xml:lang="zh">获取当前重连尝试次数。</para>
    /// </summary>
    public int AttemptNumber { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the delay before the next reconnection attempt.</para>
    ///     <para xml:lang="zh">获取下次重连尝试前的延迟。</para>
    /// </summary>
    public TimeSpan Delay { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the exception from the last failed attempt, if any.</para>
    ///     <para xml:lang="zh">获取上次失败尝试的异常（如果有）。</para>
    /// </summary>
    public Exception? LastException { get; }

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
public sealed class WebSocketClosedEventArgs : EventArgs
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketClosedEventArgs" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketClosedEventArgs" /> 类的新实例。</para>
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
    public WebSocketClosedEventArgs(WebSocketCloseStatus? closeStatus, string? closeDescription, bool initiatedByClient)
    {
        CloseStatus = closeStatus;
        CloseDescription = closeDescription;
        InitiatedByClient = initiatedByClient;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the WebSocket close status code.</para>
    ///     <para xml:lang="zh">获取 WebSocket 关闭状态码。</para>
    /// </summary>
    public WebSocketCloseStatus? CloseStatus { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the close status description.</para>
    ///     <para xml:lang="zh">获取关闭状态描述。</para>
    /// </summary>
    public string? CloseDescription { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the close was initiated by the client.</para>
    ///     <para xml:lang="zh">获取一个值，指示是否由客户端发起关闭。</para>
    /// </summary>
    public bool InitiatedByClient { get; }
}