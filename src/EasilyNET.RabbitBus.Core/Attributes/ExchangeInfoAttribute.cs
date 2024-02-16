// ReSharper disable ClassNeverInstantiated.Global

using EasilyNET.RabbitBus.Core.Enums;

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 应用交换机队列等参数
/// <a href="https://www.rabbitmq.com/getstarted.html">Exchanges</a>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class ExchangeInfoAttribute(EModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true) : ExchangeAttribute(workModel, routingKey, queue, enable)
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; } = workModel switch
    {
        EModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "amq.fanout" : exchangeName,
        EModel.Routing          => string.IsNullOrWhiteSpace(exchangeName) ? "amq.direct" : exchangeName,
        EModel.Topics           => string.IsNullOrWhiteSpace(exchangeName) ? "amq.topic" : exchangeName,
        EModel.Delayed          => ExchangeNameCheck(exchangeName),
        _                       => ""
    };
}