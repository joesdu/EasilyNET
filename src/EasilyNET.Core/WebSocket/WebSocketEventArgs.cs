using System.Buffers;
using System.Net.WebSockets;

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Event arguments for WebSocket state changes.</para>
///     <para xml:lang="zh">WebSocket 状态变更事件参数。</para>
/// </summary>
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
///     <para xml:lang="en">
///     Event arguments for a received WebSocket message. Implements <see cref="IDisposable" /> to return the internal
///     <see cref="System.Buffers.ArrayPool{T}" /> buffer. The buffer is owned and disposed by
///     <see cref="ManagedWebSocketClient" /> immediately after all event subscribers return — <see cref="Data" /> is only
///     valid for the duration of the event callback and must not be stored or accessed after the handler returns.
///     </para>
///     <para xml:lang="zh">
///     已接收的 WebSocket 消息事件参数。实现 <see cref="IDisposable" /> 以归还内部
///     <see cref="System.Buffers.ArrayPool{T}" /> 缓冲区。缓冲区由 <see cref="ManagedWebSocketClient" />
///     在所有事件订阅者返回后统一释放——<see cref="Data" /> 仅在事件回调期间有效，处理函数返回后不得存储或访问。
///     </para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketMessageReceivedEventArgs" /> class.</para>
///     <para xml:lang="zh">初始化 <see cref="WebSocketMessageReceivedEventArgs" /> 类的新实例。</para>
/// </remarks>
/// <param name="data">
///     <para xml:lang="en">The received message data.</para>
///     <para xml:lang="zh">接收到的消息数据。</para>
/// </param>
/// <param name="messageType">
///     <para xml:lang="en">The type of the WebSocket message.</para>
///     <para xml:lang="zh">WebSocket 消息类型。</para>
/// </param>
/// <param name="endOfMessage">
///     <para xml:lang="en">Whether this is the end of the message.</para>
///     <para xml:lang="zh">是否为消息结尾。</para>
/// </param>
public sealed class WebSocketMessageReceivedEventArgs(ReadOnlyMemory<byte> data, WebSocketMessageType messageType, bool endOfMessage) : EventArgs, IDisposable
{
    private byte[]? _rentedArray;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Initializes a new instance with an associated <see cref="ArrayPool{T}" />-rented buffer
    ///     that will be returned on <see cref="Dispose" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     使用关联的 <see cref="ArrayPool{T}" /> 租用缓冲区初始化新实例，该缓冲区将在 <see cref="Dispose" /> 时归还。
    ///     </para>
    /// </summary>
    internal WebSocketMessageReceivedEventArgs(ReadOnlyMemory<byte> data, WebSocketMessageType messageType, bool endOfMessage, byte[]? rentedArray)
        : this(data, messageType, endOfMessage)
    {
        _rentedArray = rentedArray;
    }

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

    /// <summary>
    ///     <para xml:lang="en">
    ///     Returns the rented buffer to <see cref="System.Buffers.ArrayPool{T}" />. Called by <see cref="ManagedWebSocketClient" />
    ///     after all subscribers have returned.
    ///     </para>
    ///     <para xml:lang="zh">将租用的缓冲区归还给 <see cref="System.Buffers.ArrayPool{T}" />。由 <see cref="ManagedWebSocketClient" /> 在所有订阅者返回后调用。</para>
    /// </summary>
    public void Dispose()
    {
        if (_rentedArray is null)
        {
            return;
        }
        ArrayPool<byte>.Shared.Return(_rentedArray);
        _rentedArray = null;
    }
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