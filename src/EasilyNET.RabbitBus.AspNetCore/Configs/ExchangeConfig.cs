using EasilyNET.RabbitBus.Core.Enums;

namespace EasilyNET.RabbitBus.AspNetCore.Configs;

/// <summary>
///     <para xml:lang="en">Exchange configuration</para>
///     <para xml:lang="zh">交换机配置</para>
/// </summary>
public sealed class ExchangeConfig
{
    /// <summary>
    ///     <para xml:lang="en">Exchange name</para>
    ///     <para xml:lang="zh">交换机名称</para>
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Exchange type</para>
    ///     <para xml:lang="zh">交换机类型</para>
    /// </summary>
    public EModel Type { get; set; } = EModel.None;

    /// <summary>
    ///     <para xml:lang="en">Routing key</para>
    ///     <para xml:lang="zh">路由键</para>
    /// </summary>
    public string RoutingKey { get; set; } = string.Empty;

    /// <summary>
    ///     <para xml:lang="en">Exchange arguments</para>
    ///     <para xml:lang="zh">交换机参数</para>
    /// </summary>
    public Dictionary<string, object?> Arguments { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">Whether exchange is durable</para>
    ///     <para xml:lang="zh">交换机是否持久化</para>
    /// </summary>
    public bool Durable { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Whether exchange should auto-delete</para>
    ///     <para xml:lang="zh">交换机是否自动删除</para>
    /// </summary>
    public bool AutoDelete { get; set; }
}