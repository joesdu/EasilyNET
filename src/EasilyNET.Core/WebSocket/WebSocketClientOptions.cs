using System.Net.WebSockets;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Core.WebSocket;

/// <summary>
///     <para xml:lang="en">Configuration options for <see cref="ManagedWebSocketClient" />.</para>
///     <para xml:lang="zh"><see cref="ManagedWebSocketClient" /> 的配置选项。</para>
/// </summary>
public sealed class WebSocketClientOptions
{
    /// <summary>
    ///     <para xml:lang="en">Cached default heartbeat message bytes ("ping").</para>
    ///     <para xml:lang="zh">缓存的默认心跳消息字节（"ping"）。</para>
    /// </summary>
    private static readonly byte[] DefaultHeartbeatMessage = "ping"u8.ToArray();

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the WebSocket server URI.</para>
    ///     <para xml:lang="zh">获取或设置 WebSocket 服务器 URI。</para>
    /// </summary>
    public Uri? ServerUri { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether automatic reconnection is enabled. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否启用自动重连。默认为 <c>true</c>。</para>
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the maximum number of reconnection attempts. Default is 5. Set to -1 for infinite retries.</para>
    ///     <para xml:lang="zh">获取或设置最大重连次数。默认为 5。设置为 -1 表示无限重试。</para>
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 5;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the initial delay between reconnection attempts. Default is 1 second.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的初始延迟。默认为 1 秒。</para>
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the maximum delay between reconnection attempts. Default is 30 seconds.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的最大延迟。默认为 30 秒。</para>
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets whether application-level heartbeat is enabled. Requires server-side cooperation to recognize and handle heartbeat payloads. The server may optionally respond (for example, with a PONG or any other message type); any message received from the server is treated as activity for timeout detection. Default is
    ///     <c>false</c>.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置是否启用应用层心跳。需要服务端配合识别并处理心跳数据；服务端可以选择性地回应（例如发送 PONG 或任意类型的消息），客户端收到的任何消息都会被视为活动并用于超时检测。默认为 <c>false</c>。</para>
    /// </summary>
    public bool HeartbeatEnabled { get; set; } = false;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat interval. Default is 30 seconds.</para>
    ///     <para xml:lang="zh">获取或设置心跳间隔。默认为 30 秒。</para>
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat timeout. Default is 10 seconds.</para>
    ///     <para xml:lang="zh">获取或设置心跳超时。默认为 10 秒。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This timeout is evaluated against the time since the last successfully received message.
    ///         If no data is received within this window, the client considers the connection stale and may trigger reconnection.
    ///         Set to TimeSpan.Zero or a negative value to disable the timeout check.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         该超时基于“距离上次成功接收消息”的时间进行判断。
    ///         若在该时间窗口内未收到任何数据，客户端将认为连接可能已失活并可能触发重连。
    ///         设置为 TimeSpan.Zero 或负数可禁用该超时检测。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the factory function to create heartbeat messages. Returns null to send an empty payload.</para>
    ///     <para xml:lang="zh">获取或设置创建心跳消息的工厂函数。返回 null 则发送空负载。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Note: <see cref="ClientWebSocket" /> does not expose protocol-level Ping/Pong control frames.
    ///         Heartbeats here are application-level messages (typically small binary payloads) and require server-side cooperation if you expect a reply.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         注意：<see cref="ClientWebSocket" /> 不直接暴露协议层的 Ping/Pong 控制帧。
    ///         此处心跳属于应用层消息（通常为较小的二进制负载），若希望收到回应，需要服务端配合实现。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public Func<ReadOnlyMemory<byte>>? HeartbeatMessageFactory { get; set; } = DefaultHeartbeatMessageFactory;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the connection timeout. Default is 10 seconds.</para>
    ///     <para xml:lang="zh">获取或设置连接超时。默认为 10 秒。</para>
    /// </summary>
    public TimeSpan ConnectionTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the receive buffer size in bytes. Default is 16384 (16KB).</para>
    ///     <para xml:lang="zh">获取或设置接收缓冲区大小（字节）。默认为 16384（16KB）。</para>
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 16384;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the send queue capacity. Default is 1000.</para>
    ///     <para xml:lang="zh">获取或设置发送队列容量。默认为 1000。</para>
    /// </summary>
    public int SendQueueCapacity { get; set; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the action to configure the underlying <see cref="ClientWebSocket" />.</para>
    ///     <para xml:lang="zh">获取或设置用于配置底层 <see cref="ClientWebSocket" /> 的操作。</para>
    /// </summary>
    public Action<ClientWebSocket>? ConfigureWebSocket { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether to use exponential backoff for reconnection delays. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否使用指数退避策略进行重连延迟。默认为 <c>true</c>。</para>
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether to wait for the send operation to complete and propagate errors synchronously. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否等待发送操作完成并同步传播错误。默认为 <c>true</c>。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         The internal channel always maintains message order regardless of this setting.
    ///         When set to <c>true</c>, <c>SendAsync</c> will await the actual socket send operation and throw any exceptions that occur.
    ///         When set to <c>false</c>, <c>SendAsync</c> returns as soon as the message is enqueued, and send errors are handled by the background loop (and raised via the Error event).
    ///         </para>
    ///         <para xml:lang="zh">
    ///         无论此设置如何，内部通道始终保持消息顺序。
    ///         当设置为 <c>true</c> 时，<c>SendAsync</c> 将等待实际的套接字发送操作，并抛出发生的任何异常。
    ///         当设置为 <c>false</c> 时，<c>SendAsync</c> 在消息入队后立即返回，发送错误由后台循环处理（并通过 Error 事件引发）。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public bool WaitForSendCompletion { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Default heartbeat message factory that returns "ping" as bytes.</para>
    ///     <para xml:lang="zh">默认心跳消息工厂，返回 "ping" 字节。</para>
    /// </summary>
    private static ReadOnlyMemory<byte> DefaultHeartbeatMessageFactory() => DefaultHeartbeatMessage;
}