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
    /// 延时 x-delayed-message 模式
    /// </summary>
    [Description("x-delayed-message")]
    Delayed
}
