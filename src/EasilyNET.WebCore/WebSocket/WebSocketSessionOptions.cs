using System.Net.WebSockets;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Configuration options for WebSocket session.</para>
///     <para xml:lang="zh">WebSocket 会话的配置选项。</para>
/// </summary>
public sealed class WebSocketSessionOptions
{
    /// <summary>
    ///     <para xml:lang="en">Gets or sets the capacity of the send queue. Default is 1000.</para>
    ///     <para xml:lang="zh">获取或设置发送队列的容量。默认为 1000。</para>
    /// </summary>
    public int SendQueueCapacity { get; set; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the receive buffer size in bytes. Default is 16384 (16KB).</para>
    ///     <para xml:lang="zh">获取或设置接收缓冲区大小（字节）。默认为 16384（16KB）。</para>
    /// </summary>
    public int ReceiveBufferSize { get; set; } = 16384;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the maximum allowed inbound message size in bytes. Default is 4MB (4 * 1024 * 1024).
    ///     Messages exceeding this size cause the connection to be closed with <see cref="WebSocketCloseStatus.MessageTooBig" /> to prevent memory
    ///     exhaustion by malicious or buggy clients. Set to 0 or a negative value to disable the limit (not recommended).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取或设置允许的最大入站消息大小（字节）。默认为 4MB (4 * 1024 * 1024)。
    ///     超过此大小的消息会导致连接以 <see cref="WebSocketCloseStatus.MessageTooBig" /> 关闭，以防止恶意或有缺陷的客户端耗尽服务端内存。
    ///     设置为 0 或负数可禁用该限制（不推荐）。
    ///     </para>
    /// </summary>
    public long MaxMessageSize { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the capacity of the internal queue that decouples message receiving from <see cref="WebSocketHandler.OnMessageAsync" /> dispatch.
    ///     Default is 1024.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置用于将消息接收与 <see cref="WebSocketHandler.OnMessageAsync" /> 分发解耦的内部队列容量。默认为 1024。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Decoupling ensures a slow message handler cannot stall the receive loop. When the queue is full, the receive loop applies backpressure by
    ///         waiting for space.
    ///         </para>
    ///         <para xml:lang="zh">解耦确保慢处理器不会阻塞接收循环。当队列已满时，接收循环通过等待空间产生背压。</para>
    ///     </remarks>
    /// </summary>
    public int ReceiveDispatchQueueCapacity { get; set; } = 1024;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the close timeout. Default is 5 seconds.</para>
    ///     <para xml:lang="zh">获取或设置关闭超时。默认为 5 秒。</para>
    /// </summary>
    public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the protocol-level keep-alive (PING) interval applied when accepting the connection. Leave <c>null</c> to use the server's global
    ///     default configured via <c>app.UseWebSockets(...)</c>.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置接受连接时应用的协议层保活（PING）间隔。保持 <c>null</c> 则使用 <c>app.UseWebSockets(...)</c> 配置的服务端全局默认值。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This is the transport-level keep-alive handled by the runtime. Combine with <see cref="KeepAliveTimeout" /> to enable automatic
    ///         dead-connection detection (PING/PONG strategy).
    ///         </para>
    ///         <para xml:lang="zh">这是由运行时处理的传输层保活。与 <see cref="KeepAliveTimeout" /> 配合即可启用自动死连接检测（PING/PONG 策略）。</para>
    ///     </remarks>
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the protocol-level keep-alive timeout (.NET 8+) applied when accepting the connection. When combined with
    ///     <see cref="KeepAliveInterval" />, the runtime uses a PING/PONG strategy and aborts the connection if no PONG is received within this duration,
    ///     providing automatic dead-connection detection at the protocol layer. Leave <c>null</c> to use the server's global default.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取或设置接受连接时应用的协议层保活超时（.NET 8+）。与 <see cref="KeepAliveInterval" /> 配合时，运行时采用 PING/PONG 策略：超时内未收到 PONG 则中止连接，
    ///     从而在协议层自动完成死连接检测。保持 <c>null</c> 则使用服务端全局默认值。
    ///     </para>
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; set; }
}
