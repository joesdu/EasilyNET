namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Queue configuration</para>
///     <para xml:lang="zh">队列配置</para>
/// </summary>
public sealed class QueueConfig
{
    /// <summary>
    ///     <para xml:lang="en">Queue name</para>
    ///     <para xml:lang="zh">队列名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Queue arguments</para>
    ///     <para xml:lang="zh">队列参数</para>
    /// </summary>
    public Dictionary<string, object?> Arguments { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether queue is durable</para>
    ///     <para xml:lang="zh">队列是否持久化</para>
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether queue is exclusive</para>
    ///     <para xml:lang="zh">队列是否独占</para>
    /// </summary>
    public bool Exclusive { get; set; }

    /// <summary>
    ///     <para xml:lang="en">Whether queue should auto-delete</para>
    ///     <para xml:lang="zh">队列是否自动删除</para>
    /// </summary>
    public bool AutoDelete { get; set; }
}