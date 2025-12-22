using System.Net.WebSockets;

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Represents a WebSocket message to be sent.</para>
///     <para xml:lang="zh">表示要发送的 WebSocket 消息。</para>
/// </summary>
public readonly struct WebSocketMessage
{
    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="WebSocketMessage" /> struct.</para>
    ///     <para xml:lang="zh">初始化 <see cref="WebSocketMessage" /> 结构的新实例。</para>
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
    /// <param name="completionSource">
    ///     <para xml:lang="en">The completion source for tracking send completion.</para>
    ///     <para xml:lang="zh">用于跟踪发送完成的完成源。</para>
    /// </param>
    /// <param name="rentedArray">
    ///     <para xml:lang="en">The rented array that needs to be returned to the pool.</para>
    ///     <para xml:lang="zh">获取需要返回到池的租用数组。</para>
    /// </param>
    public WebSocketMessage(ReadOnlyMemory<byte> data, WebSocketMessageType messageType, bool endOfMessage, TaskCompletionSource<bool>? completionSource = null, byte[]? rentedArray = null)
    {
        Data = data;
        MessageType = messageType;
        EndOfMessage = endOfMessage;
        CompletionSource = completionSource;
        RentedArray = rentedArray;
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the message data.</para>
    ///     <para xml:lang="zh">获取消息数据。</para>
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

    /// <summary>
    ///     <para xml:lang="en">Gets the completion source for tracking send completion.</para>
    ///     <para xml:lang="zh">获取用于跟踪发送完成的完成源。</para>
    /// </summary>
    public TaskCompletionSource<bool>? CompletionSource { get; }

    /// <summary>
    ///     <para xml:lang="en">Gets the rented array that needs to be returned to the pool.</para>
    ///     <para xml:lang="zh">获取需要返回到池的租用数组。</para>
    /// </summary>
    public byte[]? RentedArray { get; }
}