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
    ///     <para xml:lang="en">Gets or sets the initial delay between reconnection attempts in milliseconds. Default is 1000ms.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的初始延迟（毫秒）。默认为 1000 毫秒。</para>
    /// </summary>
    public int ReconnectDelayMs { get; set; } = 1000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the maximum delay between reconnection attempts in milliseconds. Default is 30000ms.</para>
    ///     <para xml:lang="zh">获取或设置重连尝试之间的最大延迟（毫秒）。默认为 30000 毫秒。</para>
    /// </summary>
    public int MaxReconnectDelayMs { get; set; } = 30000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets whether heartbeat is enabled. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否启用心跳。默认为 <c>true</c>。</para>
    /// </summary>
    public bool HeartbeatEnabled { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat interval in milliseconds. Default is 30000ms.</para>
    ///     <para xml:lang="zh">获取或设置心跳间隔（毫秒）。默认为 30000 毫秒。</para>
    /// </summary>
    public int HeartbeatIntervalMs { get; set; } = 30000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the heartbeat timeout in milliseconds. Default is 10000ms.</para>
    ///     <para xml:lang="zh">获取或设置心跳超时（毫秒）。默认为 10000 毫秒。</para>
    /// </summary>
    public int HeartbeatTimeoutMs { get; set; } = 10000;

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the factory function to create heartbeat messages. Returns null to use default ping frame.</para>
    ///     <para xml:lang="zh">获取或设置创建心跳消息的工厂函数。返回 null 则使用默认的 ping 帧。</para>
    /// </summary>
    public Func<ReadOnlyMemory<byte>>? HeartbeatMessageFactory { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the connection timeout in milliseconds. Default is 10000ms.</para>
    ///     <para xml:lang="zh">获取或设置连接超时（毫秒）。默认为 10000 毫秒。</para>
    /// </summary>
    public int ConnectionTimeoutMs { get; set; } = 10000;

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
    ///     <para xml:lang="en">Gets or sets whether to keep the message order when sending. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置发送时是否保持消息顺序。默认为 <c>true</c>。</para>
    /// </summary>
    public bool KeepMessageOrder { get; set; } = true;
}