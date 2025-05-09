using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
///     <para xml:lang="en">Applies exchange, queue, and other parameters</para>
///     <para xml:lang="zh">应用交换机、队列等参数</para>
///     <para xml:lang="en">
///     Note: When workModel is None and bindDlx is true, this combination is not supported because the default exchange type is
///     already created and does not support binding a dead-letter exchange
///     </para>
///     <para xml:lang="zh">注意：当 workModel 为 None 并且 bindDlx 为 true 时，这是不被支持的组合，因为默认交换机类型已经创建不支持再绑定死信交换机</para>
///     <a href="https://www.rabbitmq.com/getstarted.html"></a>
/// </summary>
/// <param name="workModel">
///     <para xml:lang="en">Work mode <see cref="EModel" /></para>
///     <para xml:lang="zh">工作模式 <see cref="EModel" /></para>
/// </param>
/// <param name="exchangeName">
///     <para xml:lang="en">Exchange name</para>
///     <para xml:lang="zh">交换机名称</para>
/// </param>
/// <param name="routingKey">
///     <para xml:lang="en">Routing key</para>
///     <para xml:lang="zh">路由键</para>
/// </param>
/// <param name="queue">
///     <para xml:lang="en">Queue name</para>
///     <para xml:lang="zh">队列名称</para>
/// </param>
/// <param name="enable">
///     <para xml:lang="en">Whether to enable</para>
///     <para xml:lang="zh">是否启用</para>
/// </param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExchangeAttribute(EModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">Exchange name</para>
    ///     <para xml:lang="zh">交换机名称</para>
    /// </summary>
    public string ExchangeName { get; } = workModel switch
    {
        EModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "amq.fanout" : exchangeName,
        EModel.Routing => string.IsNullOrWhiteSpace(exchangeName) ? "amq.direct" : exchangeName,
        EModel.Topics => string.IsNullOrWhiteSpace(exchangeName) ? "amq.topic" : exchangeName,
        EModel.Delayed => string.IsNullOrWhiteSpace(exchangeName) ? "amq.delayed" : exchangeName,
        EModel.None => "",
        _ => throw new ArgumentOutOfRangeException(nameof(workModel), workModel, null)
    };

    /// <summary>
    ///     <para xml:lang="en">Work mode</para>
    ///     <para xml:lang="zh">工作模式</para>
    /// </summary>
    public EModel WorkModel { get; } = workModel;

    /// <summary>
    ///     <para xml:lang="en">Routing key (used in conjunction with queue name)</para>
    ///     <para xml:lang="zh">路由键（与队列名称配合使用）</para>
    /// </summary>
    public string RoutingKey { get; } = workModel switch
    {
        EModel.None => queue,
        _ => routingKey
    };

    /// <summary>
    ///     <para xml:lang="en">Queue name (used in conjunction with routing key)</para>
    ///     <para xml:lang="zh">队列名称（与路由键配合使用）</para>
    /// </summary>
    public string Queue { get; } = queue;

    /// <summary>
    ///     <para xml:lang="en">Whether to enable the queue</para>
    ///     <para xml:lang="zh">是否启用队列</para>
    /// </summary>
    public bool Enable { get; } = enable;
}