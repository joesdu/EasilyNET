using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 死信队列配置
/// <a href="https://www.rabbitmq.com/dlx.html">Dead Letter Exchanges</a>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeadLetterAttribute(EWorkModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true) : Attribute
{
    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; } = workModel switch
    {
        EWorkModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.fanout" : exchangeName,
        EWorkModel.Routing          => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.direct" : exchangeName,
        EWorkModel.Topics           => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.topic" : exchangeName,
        _                           => ExchangeNameCheck(exchangeName)
    };

    /// <summary>
    /// 交换机模式
    /// </summary>
    public EWorkModel WorkModel { get; } = workModel;

    /// <summary>
    /// 路由键《路由键和队列名称配合使用》
    /// </summary>
    public string RoutingKey { get; } = workModel switch
    {
        EWorkModel.None => queue,
        _               => routingKey
    };

    /// <summary>
    /// 队列名称《队列名称和路由键配合使用》
    /// </summary>
    public string Queue { get; } = queue;

    /// <summary>
    /// 是否启用队列
    /// </summary>
    public bool Enable { get; } = enable;

    private static string ExchangeNameCheck(string exchangeName)
    {
#if NET7_0_OR_GREATER
        ArgumentException.ThrowIfNullOrEmpty(exchangeName, nameof(exchangeName));
#else
        if (string.IsNullOrWhiteSpace(exchangeName)) throw new ArgumentNullException(nameof(exchangeName));
#endif
        return exchangeName;
    }
}