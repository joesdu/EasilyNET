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
    ///     <para xml:lang="en">(Headers) Headers mode, routes messages based on header attributes matching</para>
    ///     <para xml:lang="zh">(Headers)头部模式,根据消息头属性匹配进行路由</para>
    /// </summary>
    [Description("headers")]
    Headers
}