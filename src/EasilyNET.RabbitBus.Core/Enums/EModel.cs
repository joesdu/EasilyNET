using System.ComponentModel;

// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Core.Enums;

/// <summary>
///     <para xml:lang="en">Exchange work mode</para>
///     <para xml:lang="zh">交换机工作模式</para>
/// </summary>
public enum EModel
{
    /// <summary>
    ///     <para xml:lang="en">Do not set the exchange, use the default exchange</para>
    ///     <para xml:lang="zh">不设置交换机,使用默认交换机</para>
    /// </summary>
    [Description("")]
    None,

    /// <summary>
    ///     <para xml:lang="en">(Publish/Subscribe) Publish/Subscribe mode</para>
    ///     <para xml:lang="zh">(Publish/Subscribe)发布/订阅模式</para>
    /// </summary>
    [Description("fanout")]
    PublishSubscribe,

    /// <summary>
    ///     <para xml:lang="en">(Routing) Routing mode</para>
    ///     <para xml:lang="zh">(Routing)路由模式</para>
    /// </summary>
    [Description("direct")]
    Routing,

    /// <summary>
    ///     <para xml:lang="en">(Topics) Topics mode</para>
    ///     <para xml:lang="zh">(Topics)主题模式</para>
    /// </summary>
    [Description("topic")]
    Topics,

    /// <summary>
    ///     <para xml:lang="en">Delayed x-delayed-message mode</para>
    ///     <para xml:lang="zh">延时 x-delayed-message 模式</para>
    /// </summary>
    [Description("x-delayed-message")]
    Delayed
}