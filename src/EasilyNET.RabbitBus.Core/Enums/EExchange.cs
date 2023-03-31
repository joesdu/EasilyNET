using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Core.Enums;

/// <summary>
/// 交换机类型
/// </summary>
public enum EExchange
{
    /// <summary>
    /// 路由模式
    /// </summary>
    [Description("direct")]
    Routing,

    /// <summary>
    /// 发布/订阅模式
    /// </summary>
    [Description("fanout")]
    Publish,

    /// <summary>
    /// 主题模式
    /// </summary>
    [Description("topic")]
    Topic,

    /// <summary>
    /// 延时x-delayed-message模式,必须添加RabbitMQArg特性,键:x-delayed-type,值为RabbitMQ所支持的.
    /// </summary>
    [Description("x-delayed-message")]
    Delayed
}