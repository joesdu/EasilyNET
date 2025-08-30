namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">QoS configuration</para>
///     <para xml:lang="zh">QoS配置</para>
/// </summary>
public sealed class QosConfig
{
    /// <summary>
    ///     <para xml:lang="en">Prefetch size</para>
    ///     <para xml:lang="zh">预取大小</para>
    /// </summary>
    public uint PrefetchSize { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Prefetch count</para>
    ///     <para xml:lang="zh">预取数量</para>
    /// </summary>
    public ushort PrefetchCount { get; set; } = 1;

    /// <summary>
    ///     <para xml:lang="en">Whether QoS is global</para>
    ///     <para xml:lang="zh">QoS是否全局</para>
    /// </summary>
    public bool Global { get; set; }
}