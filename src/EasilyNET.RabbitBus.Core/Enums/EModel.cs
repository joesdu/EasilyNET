using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Core.Enums;

/// <summary>
/// 交换机工作模式
/// </summary>
public enum EModel
{
    /// <summary>
    /// 不设置交换机,使用默认交换机
    /// </summary>
    [Description("")]
    None,

    /// <summary>
    /// (Publish/Subscribe)发布/订阅模式
    /// </summary>
    [Description("fanout")]
    PublishSubscribe,

    /// <summary>
    /// (Routing)路由模式
    /// </summary>
    [Description("direct")]
    Routing,

    /// <summary>
    /// (Topics)主题模式
    /// </summary>
    [Description("topic")]
    Topics,

    /// <summary>
    /// 延时x-delayed-message模式,必须添加<code>RabbitExchangeArgAttribute</code>特性,键:x-delayed-type,值为RabbitMQ所支持的.
    /// </summary>
    [Description("x-delayed-message")]
    Delayed
}
