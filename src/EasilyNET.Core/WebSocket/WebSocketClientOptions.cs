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
    ///     <para xml:lang="en">Gets or sets the WebSocket keep-alive (PING) interval.</para>
    ///     <para xml:lang="zh">获取或设置 WebSocket 保活（PING）间隔。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This drives transport-level keep-alive handled by the runtime. When set together with <see cref="KeepAliveTimeout" />, the runtime uses a
    ///         PING/PONG strategy and aborts the connection if no PONG arrives within the timeout (true protocol-level dead-connection detection). When set
    ///         alone, the runtime only sends unsolicited PONG frames to keep intermediaries alive and does NOT detect a dead peer.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         这是由运行时处理的传输层保活。与 <see cref="KeepAliveTimeout" /> 同时设置时，运行时采用 PING/PONG 策略：发出 PING 后若在超时内未收到 PONG 则中止连接（真正的协议层死连接检测）。
    ///         仅设置本项而不设置超时时，运行时只发送 PONG 帧以维持中间设备存活，<b>不会</b>检测对端是否已死。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public TimeSpan? KeepAliveInterval { get; init; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets or sets the protocol-level keep-alive timeout (.NET 8+). When combined with <see cref="KeepAliveInterval" />, the runtime sends a PING and
    ///     aborts the connection if no PONG is received within this duration, providing automatic dead-connection detection at the protocol layer without
    ///     any application messages. Leave <c>null</c> to keep the runtime default (no timeout).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取或设置协议层保活超时（.NET 8+）。与 <see cref="KeepAliveInterval" /> 同时设置时，运行时会发送 PING 并在此时间内未收到 PONG 时中止连接，
    ///     从而在协议层自动完成死连接检测，无需任何应用层消息。保持 <c>null</c> 则使用运行时默认值（不检测超时）。
    ///     </para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         This is the recommended way to keep connections alive and detect dead peers. When the runtime aborts the connection on timeout, the receive
    ///         loop observes the error and the library's auto-reconnect takes over.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         这是推荐的保活与死连接检测方式。运行时因超时中止连接时，接收循环会观察到该错误，并由库的自动重连接管。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public TimeSpan? KeepAliveTimeout { get; init; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the requested SubProtocols.</para>
    ///     <para xml:lang="zh">请求的 SubProtocol 列表</para>
    /// </summary>
    public IReadOnlyList<string>? RequestedSubProtocols { get; init; }

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
    ///     <para xml:lang="en">
    ///     Gets or sets the additional grace period used by <see cref="ManagedWebSocketClient.DisposeAsync" /> after the initial lock
    ///     wait times out. Default is 25 seconds.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置 <see cref="ManagedWebSocketClient.DisposeAsync" /> 在首次等待锁超时后使用的额外宽限时间。默认为 25 秒。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     If the lock is still unavailable after the total bounded wait, disposal falls back to best-effort cleanup and skips unsafe concurrent resource
    ///     disposal.
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
    ///     <para xml:lang="en">
    ///     Gets or sets the capacity of the internal queue that decouples message receiving from user callback dispatch. Default is 1024.
    ///     </para>
    ///     <para xml:lang="zh">获取或设置用于将消息接收与用户回调分发解耦的内部队列容量。默认为 1024。</para>
    ///     <remarks>
    ///         <para xml:lang="en">
    ///         Received messages are enqueued here and dispatched to <see cref="ManagedWebSocketClient.MessageReceived" /> /
    ///         <see cref="ManagedWebSocketClient.MessageReceivedAsync" /> by a dedicated loop, so a slow handler no longer blocks the receive loop or
    ///         delays heartbeat liveness tracking. When the queue is full (handlers cannot keep up), the receive loop applies backpressure by waiting
    ///         for space, bounding memory usage.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         接收到的消息先进入此队列，再由专门的分发循环投递给 <see cref="ManagedWebSocketClient.MessageReceived" /> /
    ///         <see cref="ManagedWebSocketClient.MessageReceivedAsync" />，因此慢处理器不再阻塞接收循环，也不会干扰心跳活性检测。
    ///         当队列已满（处理器跟不上消费速度）时，接收循环会通过等待空间产生背压，从而限制内存占用。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public int ReceiveDispatchQueueCapacity { get; init; } = 1024;

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
    ///         (and raised via the Error event). Messages still pending in the queue during connection loss may be dropped; subscribe to
    ///         <see cref="ManagedWebSocketClient.Error" /> to observe those failures.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         无论此设置如何，内部通道始终保持消息顺序。
    ///         当设置为 <c>true</c> 时，<c>SendAsync</c> 将等待实际的套接字发送操作，并抛出发生的任何异常。
    ///         当设置为 <c>false</c> 时，<c>SendAsync</c> 在消息入队后立即返回，发送错误由后台循环处理（并通过 Error 事件引发）。连接丢失时仍停留在队列中的消息可能会被丢弃；请订阅
    ///         <see cref="ManagedWebSocketClient.Error" /> 以观察这些失败。
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
        if (KeepAliveTimeout is { } keepAliveTimeout)
        {
            // .NET 8+：与 KeepAliveInterval 配合时启用 PING/PONG 死连接检测。
            clientWebSocket.Options.KeepAliveTimeout = keepAliveTimeout;
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
        if (ReceiveDispatchQueueCapacity <= 0)
        {
            throw new InvalidOperationException($"{nameof(ReceiveDispatchQueueCapacity)} must be greater than zero.");
        }
        if (ReconnectDelay <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ReconnectDelay)} must be positive.");
        }
        if (UseExponentialBackoff && MaxReconnectDelay < ReconnectDelay)
        {
            throw new InvalidOperationException($"{nameof(MaxReconnectDelay)} must be greater than or equal to {nameof(ReconnectDelay)} when {nameof(UseExponentialBackoff)} is enabled.");
        }
        if (MaxReconnectAttempts < -1)
        {
            throw new InvalidOperationException($"{nameof(MaxReconnectAttempts)} must be -1 (infinite) or a non-negative integer.");
        }
        if (ConnectionTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ConnectionTimeout)} must be positive.");
        }
        if (KeepAliveInterval is { } interval && interval < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(KeepAliveInterval)} must be null or non-negative.");
        }
        if (KeepAliveTimeout is { } keepAliveTimeout && keepAliveTimeout < TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(KeepAliveTimeout)} must be null or non-negative.");
        }
    }
}