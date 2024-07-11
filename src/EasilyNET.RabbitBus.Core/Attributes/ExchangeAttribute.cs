using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 应用交换机队列等参数
/// 注意：当 workModel 为 None 并且 bindDlx 为 true 时，这是不被支持的组合，因为默认交换机类型已经创建不支持再绑定死信交换机。
/// <a href="https://www.rabbitmq.com/getstarted.html"></a>
/// </summary>
/// <param name="workModel">工作模式 <see cref="EModel" /></param>
/// <param name="exchangeName">交换机名称</param>
/// <param name="routingKey">路由键</param>
/// <param name="queue">队列名称</param>
/// <param name="enable">是否启用</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExchangeAttribute(EModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true) : Attribute
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; } = workModel switch
    {
        EModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "amq.fanout" : exchangeName,
        EModel.Routing          => string.IsNullOrWhiteSpace(exchangeName) ? "amq.direct" : exchangeName,
        EModel.Topics           => string.IsNullOrWhiteSpace(exchangeName) ? "amq.topic" : exchangeName,
        EModel.Delayed          => string.IsNullOrWhiteSpace(exchangeName) ? "amq.delayed" : exchangeName,
        EModel.None             => "",
        _                       => throw new ArgumentOutOfRangeException(nameof(workModel), workModel, null)
    };

    /// <summary>
    /// 交换机模式
    /// </summary>
    public EModel WorkModel { get; } = workModel;

    /// <summary>
    /// 路由键《路由键和队列名称配合使用》
    /// </summary>
    public string RoutingKey { get; } = workModel switch
    {
        EModel.None => queue,
        _           => routingKey
    };

    /// <summary>
    /// 队列名称《队列名称和路由键配合使用》
    /// </summary>
    public string Queue { get; } = queue;

    /// <summary>
    /// 是否启用队列
    /// </summary>
    public bool Enable { get; } = enable;
}