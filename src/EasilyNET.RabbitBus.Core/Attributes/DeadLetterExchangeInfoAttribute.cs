using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 死信队列配置
/// <a href="https://www.rabbitmq.com/dlx.html">Dead Letter Exchanges</a>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeadLetterExchangeInfoAttribute(EModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true) : ExchangeAttribute(workModel, routingKey, queue, enable)
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; } = workModel switch
    {
        EModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.fanout" : exchangeName,
        EModel.Routing          => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.direct" : exchangeName,
        EModel.Topics           => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.topic" : exchangeName,
        _                       => ExchangeNameCheck(exchangeName)
    };
}