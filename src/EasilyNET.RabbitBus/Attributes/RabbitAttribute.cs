using EasilyNET.RabbitBus.Enums;
using EasilyNET.RabbitBus.Extensions;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Attributes;

/// <summary>
/// 应用交换机队列等参数
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class RabbitAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="exchange">交换机</param>
    /// <param name="exchangeType">交换机类型</param>
    /// <param name="routingKey">路由键</param>
    /// <param name="queue">队列名</param>
    /// <param name="enable">是否启用</param>
    public RabbitAttribute(string exchange, EExchange exchangeType, string routingKey, string? queue = null, bool enable = true)
    {
        Exchange = exchange;
        Type = exchangeType.ToDescription() ?? "direct";
        RoutingKey = routingKey;
        Queue = queue;
        Enable = enable;
    }

    /// <summary>
    /// 交换机
    /// </summary>
    public string Exchange { get; }

    /// <summary>
    /// 交换机模式
    /// </summary>
    public string Type { get; }

    /// <summary>
    /// 路由键《路由键和队列名称配合使用》
    /// </summary>
    public string RoutingKey { get; }

    /// <summary>
    /// 队列名称《队列名称和路由键配合使用》
    /// </summary>
    public string? Queue { get; set; }

    /// <summary>
    /// 是否启用队列
    /// </summary>
    public bool Enable { get; }
}