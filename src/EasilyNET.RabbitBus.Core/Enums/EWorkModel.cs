using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Core.Enums;

/// <summary>
/// 工作模式
/// </summary>
public enum EWorkModel
{
    /// <summary>
    /// HelloWorld模式
    /// </summary>
    [Description("")]
    HelloWorld,

    /// <summary>
    /// WorkQueues模式
    /// </summary>
    [Description("")]
    WorkQueues,

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
    /// RPC模式
    /// </summary>
    [Description("")]
    RPC,

    /// <summary>
    /// PublisherConfirms模式
    /// </summary>
    [Description("")]
    PublisherConfirms,

    /// <summary>
    /// 延时x-delayed-message模式,必须添加<code>RabbitExchangeArgAttribute</code>特性,键:x-delayed-type,值为RabbitMQ所支持的.
    /// </summary>
    [Description("x-delayed-message")]
    Delayed
}