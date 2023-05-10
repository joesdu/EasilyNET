using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.RabbitBus.Core.Attributes;

/// <summary>
/// 死信队列配置
/// <a href="https://www.rabbitmq.com/dlx.html">Dead Letter Exchanges</a>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class DeadLetterAttribute : Attribute
{
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="workModel">工作模式</param>
    /// <param name="exchangeName">交换机名,若是为空,则使用默认交换机</param>
    /// <param name="routingKey">路由键</param>
    /// <param name="queue">队列名</param>
    /// <param name="enable">是否启用</param>
    public DeadLetterAttribute(EWorkModel workModel, string exchangeName = "", string routingKey = "", string queue = "", bool enable = true)
    {
        ExchangeName = workModel switch
        {
            EWorkModel.PublishSubscribe => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.fanout" : exchangeName,
            EWorkModel.Routing          => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.direct" : exchangeName,
            EWorkModel.Topics           => string.IsNullOrWhiteSpace(exchangeName) ? "xdl.amq.topic" : exchangeName,
            _                           => ExchangeNameCheck(exchangeName)
        };
        RoutingKey = workModel switch
        {
            EWorkModel.None => queue,
            _               => routingKey
        };
        WorkModel = workModel;
        Queue = queue;
        Enable = enable;
    }

    /// <summary>
    /// 交换机名称
    /// </summary>
    public string ExchangeName { get; }

    /// <summary>
    /// 交换机模式
    /// </summary>
    public EWorkModel WorkModel { get; }

    /// <summary>
    /// 路由键《路由键和队列名称配合使用》
    /// </summary>
    public string RoutingKey { get; }

    /// <summary>
    /// 队列名称《队列名称和路由键配合使用》
    /// </summary>
    public string Queue { get; }

    /// <summary>
    /// 是否启用队列
    /// </summary>
    public bool Enable { get; }

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