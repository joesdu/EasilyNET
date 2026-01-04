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
    ///     <para xml:lang="en">Gets or sets whether heartbeat (keep-alive) is enabled. Default is <c>true</c>.</para>
    ///     <para xml:lang="zh">获取或设置是否启用心跳（保活）。默认为 <c>true</c>。</para>
    /// </summary>
    public bool HeartbeatEnabled { get; set; } = true;

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
    ///         If no data is received within this window after a heartbeat is sent, the connection is considered stale.
    ///         Set to <see cref="TimeSpan.Zero" /> to disable timeout checking.
    ///         </para>
    ///         <para xml:lang="zh">
    ///         如果在心跳发送后的此时间窗口内未收到任何数据，则认为连接已失效。
    ///         设置为 <see cref="TimeSpan.Zero" /> 可禁用超时检测。
    ///         </para>
    ///     </remarks>
    /// </summary>
    public TimeSpan HeartbeatTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the factory function to create heartbeat messages. Returns null to skip sending.</para>
    ///     <para xml:lang="zh">获取或设置创建心跳消息的工厂函数。返回 null 则跳过发送。</para>
    /// </summary>
    public Func<ReadOnlyMemory<byte>>? HeartbeatMessageFactory { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Gets or sets the close timeout. Default is 5 seconds.</para>
    ///     <para xml:lang="zh">获取或设置关闭超时。默认为 5 秒。</para>
    /// </summary>
    public TimeSpan CloseTimeout { get; set; } = TimeSpan.FromSeconds(5);
}