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
}