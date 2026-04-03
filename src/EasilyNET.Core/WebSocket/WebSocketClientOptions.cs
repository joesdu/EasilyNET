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
    ///     <para xml:lang="en">Cached default heartbeat response message bytes ("pong").</para>
    ///     <para xml:lang="zh">缓存的默认心跳响应消息字节（"pong"）。</para>
    /// </summary>
    private static readonly byte[] DefaultHeartbeatResponseMessage = "pong"u8.ToArray();

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the WebSocket server URI.</para>
    ///     <para xml:lang="zh">获取或设置 WebSocket 服务器 URI。</para>
    /// </summary>
    public Uri? ServerUri { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether automatic reconnection is enabled. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否启用自动重连。默认为 <c>true</c>。</para>
    /// </summary>
    public bool AutoReconnect { get; init; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the maximum number of reconnection attempts. Default is 5. Set to -1 for infinite retries.</para>
    ///     <para xml:lang="zh">获取或设置最大重连次数。默认为 5。设置为 -1 表示无限重试。</para>
    /// </summary>
    public int MaxReconnectAttempts { get; init; } = 5;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the initial delay between reconnection attempts. Default is 1 second.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的初始延迟。默认为 1 秒。</para>
    /// </summary>
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the maximum delay between reconnection attempts. Default is 30 seconds.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的最大延迟。默认为 30 秒。</para>
    /// </summary>
    public TimeSpan MaxReconnectDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the underlying TCP Keep-Alive interval.</para>
    ///     <para xml:lang="zh">获取或设置底层 TCP Keep-Alive 间隔。</para>
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the requested SubProtocols.</para>
    ///     <para xml:lang="zh">请求的 SubProtocol 列表</para>
    /// </summary>
    public IReadOnlyList<string>? RequestedSubProtocols { get; init; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets whether application-level heartbeat is enabled. Requires server-side cooperation to recognize and handle heartbeat payloads. The
    ///     server may optionally respond (for example, with a PONG or any other message type); any message received from the server is treated as activity
    ///     for timeout detection. Default is
    ///     <c>false</c>.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置是否启用应用层心跳。需要服务端配合识别并处理心跳数据；服务端可以选择性地回应（例如发送 PONG 或任意类型的消息），客户端收到的任何消息都会被视为活动并用于超时检测。默认为 <c>false</c>。</para>
    /// </summary>
    public bool HeartbeatEnabled { get; init; } = false;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat interval. Default is 30 seconds.</para>
    ///     <para xml:lang="zh">获取或设置心跳间隔。默认为 30 秒。</para>
    /// </summary>
    public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat timeout. Default is 10 seconds.</para>
    ///     <para xml:lang="zh">获取或设置心跳超时。默认为 10 秒。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This timeout is evaluated after a heartbeat is sent. If no data is received from the server
    ///         within this duration after the heartbeat was sent, the client considers the connection stale and may trigger reconnection.
    ///         Set to <see cref="TimeSpan.Zero" /> or a negative value to disable the timeout check.
    ///         <br />
    ///         <b>Important:</b> The timeout check only runs once per heartbeat tick. The actual worst-case detection latency
    ///         is <c>HeartbeatInterval + HeartbeatTimeout</c>, not just <c>HeartbeatTimeout</c>.
    ///         When <see cref="HeartbeatEnabled" /> is <c>true</c> and <see cref="HeartbeatTimeout" /> is greater than <see cref="TimeSpan.Zero" />,
    ///         this value must be less than <see cref="HeartbeatInterval" /> (this constraint is enforced by <see cref="Validate" />).
    ///         When the timeout check is disabled (zero or negative), this relation is recommended but not validated.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         该超时在发送心跳后进行评估。如果在发送心跳后的此时间段内未收到服务器的任何数据，
    ///         客户端将认为连接可能已失活并可能触发重连。
    ///         设置为 <see cref="TimeSpan.Zero" /> 或负数可禁用该超时检测。
    ///         <br />
    ///         <b>注意：</b>超时检测每个心跳周期只运行一次，实际最坏情况下的检测延迟为
    ///         <c>HeartbeatInterval + HeartbeatTimeout</c>，而非仅 <c>HeartbeatTimeout</c>。
    ///         当 <see cref="HeartbeatEnabled" /> 为 <c>true</c> 且 <see cref="HeartbeatTimeout" /> 大于 <see cref="TimeSpan.Zero" /> 时，
    ///         该值必须小于 <see cref="HeartbeatInterval" />（此约束由 <see cref="Validate" /> 方法强制校验）。
    ///         当超时检测被禁用（零或负数）时，仍然建议保持该值小于 <see cref="HeartbeatInterval" />，但不会进行强制校验。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the WebSocket message type for heartbeat messages. Default is <see cref="WebSocketMessageType.Binary" />.</para>
    ///     <para xml:lang="zh">获取或设置心跳消息的 WebSocket 消息类型。默认为 <see cref="WebSocketMessageType.Binary" />。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Use <see cref="WebSocketMessageType.Binary" /> for compatibility with most server implementations (Python, Java, etc.).
    ///         Use <see cref="WebSocketMessageType.Text" /> if your server expects text-based heartbeat messages.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         使用 <see cref="WebSocketMessageType.Binary" /> 以兼容大多数服务端实现（Python、Java 等）。
    ///         如果服务端期望文本类型的心跳消息，请使用 <see cref="WebSocketMessageType.Text" />。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public WebSocketMessageType HeartbeatMessageType { get; init; } = WebSocketMessageType.Binary;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the WebSocket message type expected for heartbeat response messages. Default is
    ///     <see cref="WebSocketMessageType.Binary" />.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置心跳响应消息期望的 WebSocket 消息类型。默认为 <see cref="WebSocketMessageType.Binary" />。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This allows the heartbeat response type to differ from the heartbeat send type.
    ///         For example, you may send heartbeats as <see cref="WebSocketMessageType.Binary" /> but the server may respond with
    ///         <see cref="WebSocketMessageType.Text" />.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         此设置允许心跳响应的消息类型与心跳发送类型不同。
    ///         例如，可以发送 <see cref="WebSocketMessageType.Binary" /> 类型的心跳，但服务端以 <see cref="WebSocketMessageType.Text" /> 类型响应。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public WebSocketMessageType HeartbeatResponseMessageType { get; init; } = WebSocketMessageType.Binary;

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
    public Func<ReadOnlyMemory<byte>>? HeartbeatMessageFactory { get; init; } = DefaultHeartbeatMessageFactory;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the expected heartbeat response message bytes. Default is "pong".</para>
    ///     <para xml:lang="zh">获取或设置期望的心跳响应消息字节。默认为 "pong"。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         When a message matching this pattern is received, it will be treated as a heartbeat response
    ///         and will NOT trigger the <see cref="ManagedWebSocketClient.MessageReceived" /> event.
    ///         Set to null or empty to disable heartbeat response filtering (all messages will be delivered to the application).
    ///         </para>
    ///         <para xml:lang="zh">
    ///         当收到与此模式匹配的消息时，将被视为心跳响应，
    ///         不会触发 <see cref="ManagedWebSocketClient.MessageReceived" /> 事件。
    ///         设置为 null 或空数组可禁用心跳响应过滤（所有消息都将传递给应用程序）。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public ReadOnlyMemory<byte> HeartbeatResponseMessage { get; init; } = DefaultHeartbeatResponseMessage;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the connection timeout. Default is 10 seconds.</para>
    ///     <para xml:lang="zh">获取或设置连接超时。默认为 10 秒。</para>
    /// </summary>
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the initial wait timeout for acquiring the internal connection lock during
    ///     <see cref="ManagedWebSocketClient.DisposeAsync" />. Default is 5 seconds.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置 <see cref="ManagedWebSocketClient.DisposeAsync" /> 期间首次等待内部连接锁的超时时间。默认为 5 秒。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     If the lock is not acquired within this timeout, the client will perform one additional bounded wait using
    ///     <see cref="DisposeLockTimeoutGracePeriod" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     如果在此时间内未获取到锁，客户端会再使用 <see cref="DisposeLockTimeoutGracePeriod" /> 进行一次有界等待。
    ///     </para>
    /// </remarks>
    public TimeSpan DisposeLockTimeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the additional grace period used by <see cref="ManagedWebSocketClient.DisposeAsync" /> after the initial lock wait times out. Default is 25 seconds.</para>
    ///     <para xml:lang="zh">获取或设置 <see cref="ManagedWebSocketClient.DisposeAsync" /> 在首次等待锁超时后使用的额外宽限时间。默认为 25 秒。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     If the lock is still unavailable after the total bounded wait, disposal falls back to best-effort cleanup and skips unsafe concurrent resource disposal.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     如果在总的有界等待时间后仍无法获取到锁，则释放会退化为 best-effort 清理，并跳过可能与并发操作冲突的资源释放。
    ///     </para>
    /// </remarks>
    public TimeSpan DisposeLockTimeoutGracePeriod { get; init; } = TimeSpan.FromSeconds(25);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the receive buffer size in bytes. Default is 16384 (16KB).</para>
    ///     <para xml:lang="zh">获取或设置接收缓冲区大小（字节）。默认为 16384（16KB）。</para>
    /// </summary>
    public int ReceiveBufferSize { get; init; } = 16384;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the send queue capacity. Default is 1000.</para>
    ///     <para xml:lang="zh">获取或设置发送队列容量。默认为 1000。</para>
    /// </summary>
    public int SendQueueCapacity { get; init; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the action to configure the underlying <see cref="ClientWebSocket" />.</para>
    ///     <para xml:lang="zh">获取或设置用于配置底层 <see cref="ClientWebSocket" /> 的操作。</para>
    /// </summary>
    public Action<ClientWebSocket>? ConfigureWebSocket { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether to use exponential backoff for reconnection delays. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否使用指数退避策略进行重连延迟。默认为 <c>true</c>。</para>
    /// </summary>
    public bool UseExponentialBackoff { get; init; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether to wait for the send operation to complete and propagate errors synchronously. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否等待发送操作完成并同步传播错误。默认为 <c>true</c>。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         The internal channel always maintains message order regardless of this setting.
    ///         When set to <c>true</c>, <c>SendAsync</c> will await the actual socket send operation and throw any exceptions that occur.
    ///         When set to <c>false</c>, <c>SendAsync</c> returns as soon as the message is enqueued, and send errors are handled by the background loop
    ///         (and raised via the Error event).
    ///         </para>
    ///         <para xml:lang="zh">
    ///         无论此设置如何，内部通道始终保持消息顺序。
    ///         当设置为 <c>true</c> 时，<c>SendAsync</c> 将等待实际的套接字发送操作，并抛出发生的任何异常。
    ///         当设置为 <c>false</c> 时，<c>SendAsync</c> 在消息入队后立即返回，发送错误由后台循环处理（并通过 Error 事件引发）。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public bool WaitForSendCompletion { get; init; } = true;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the maximum allowed message size in bytes. Default is 4MB (4 * 1024 * 1024).
    ///     Messages exceeding this size will cause the connection to be terminated to prevent memory exhaustion.
    ///     Set to 0 or a negative value to disable the size limit (not recommended).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取或设置允许的最大消息大小（字节）。默认为 4MB (4 * 1024 * 1024)。
    ///     超过此大小的消息将导致连接终止，以防止内存耗尽。
    ///     设置为 0 或负数可禁用大小限制（不推荐）。
    ///     </para>
    /// </summary>
    public long MaxMessageSize { get; init; } = 4 * 1024 * 1024;

    /// <summary>
    ///     <para xml:lang="en">Default heartbeat message factory that returns "ping" as bytes.</para>
    ///     <para xml:lang="zh">默认心跳消息工厂，返回 "ping" 字节。</para>
    /// </summary>
    private static ReadOnlyMemory<byte> DefaultHeartbeatMessageFactory() => DefaultHeartbeatMessage;

    /// <summary>
    ///     <para xml:lang="en">Applies transport-level options to the specified <see cref="ClientWebSocket" /> instance.</para>
    ///     <para xml:lang="zh">将传输层相关配置应用到指定的 <see cref="ClientWebSocket" /> 实例。</para>
    /// </summary>
    /// <param name="clientWebSocket">
    ///     <para xml:lang="en">The target client WebSocket.</para>
    ///     <para xml:lang="zh">目标客户端 WebSocket。</para>
    /// </param>
    /// <exception cref="ArgumentNullException">
    ///     <para xml:lang="en">Thrown when <paramref name="clientWebSocket" /> is <c>null</c>.</para>
    ///     <para xml:lang="zh">当 <paramref name="clientWebSocket" /> 为 <c>null</c> 时抛出。</para>
    /// </exception>
    internal void ApplyTo(ClientWebSocket clientWebSocket)
    {
        ArgumentNullException.ThrowIfNull(clientWebSocket);
        if (KeepAliveInterval is { } keepAliveInterval)
        {
            clientWebSocket.Options.KeepAliveInterval = keepAliveInterval;
        }
        if (RequestedSubProtocols is not { Count: > 0 })
        {
            return;
        }
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var subProtocol in RequestedSubProtocols)
        {
            if (string.IsNullOrWhiteSpace(subProtocol) || !seen.Add(subProtocol))
            {
                continue;
            }
            clientWebSocket.Options.AddSubProtocol(subProtocol);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Validates the options and throws <see cref="InvalidOperationException" /> if any setting is invalid.</para>
    ///     <para xml:lang="zh">验证配置选项，如有无效设置则抛出 <see cref="InvalidOperationException" />。</para>
    /// </summary>
    internal void Validate()
    {
        if (ServerUri is null)
        {
            throw new InvalidOperationException("ServerUri must be set before connecting.");
        }
        if (ReceiveBufferSize <= 0)
        {
            throw new InvalidOperationException($"{nameof(ReceiveBufferSize)} must be greater than zero.");
        }
        if (SendQueueCapacity <= 0)
        {
            throw new InvalidOperationException($"{nameof(SendQueueCapacity)} must be greater than zero.");
        }
        if (ReconnectDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ReconnectDelay)} must be positive.");
        }
        if (MaxReconnectAttempts < -1)
        {
            throw new InvalidOperationException($"{nameof(MaxReconnectAttempts)} must be -1 (infinite) or a non-negative integer.");
        }
        if (ConnectionTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ConnectionTimeout)} must be positive.");
        }
        if (HeartbeatEnabled && HeartbeatInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(HeartbeatInterval)} must be positive when heartbeat is enabled.");
        }
        if (HeartbeatEnabled && HeartbeatTimeout > TimeSpan.Zero && HeartbeatTimeout >= HeartbeatInterval)
        {
            throw new InvalidOperationException($"{nameof(HeartbeatTimeout)} must be less than {nameof(HeartbeatInterval)} to ensure the timeout can be detected within one heartbeat cycle.");
        }
        if (KeepAliveInterval is { } interval && interval < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(KeepAliveInterval)} must be null or non-negative.");
        }
    }
}