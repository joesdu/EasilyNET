using EasilyNET.RabbitBus.Configs;
using EasilyNET.RabbitBus.Core.Enums;

namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
/// 延迟地址解析器。
/// 延迟地址是阶梯路由键的最后一段,消息在最后一跳由投递交换机(topic)按 <c>#.{address}</c> 匹配到落点。
/// 发布端根据本次发布的实际路由键计算地址,消费端根据队列自身的绑定模式计算地址,
/// 二者在 topic 匹配下天然对齐,因此延迟发布与普通发布抵达同一批消费者。
/// </summary>
internal static class DelayAddressResolver
{
    private const string DefaultExchangeToken = "default";

    /// <summary>
    /// 计算发布端使用的延迟地址
    /// </summary>
    /// <param name="config">事件配置</param>
    /// <param name="routingKey">本次发布显式指定的路由键,为空则使用事件配置中的值</param>
    /// <param name="mode">地址解析模式</param>
    public static string ResolvePublishAddress(EventConfiguration config, string? routingKey, EDelayAddressMode mode)
    {
        if (!string.IsNullOrWhiteSpace(config.DelayAddress))
        {
            return Normalize(config.DelayAddress);
        }
        if (mode is EDelayAddressMode.QueueDirect)
        {
            return QueueAddress(config);
        }
        return config.Exchange.Type switch
        {
            EModel.PublishSubscribe                 => ExchangeAddress(config),
            EModel.Routing or EModel.Topics         => RoutingAddress(config, routingKey ?? config.Exchange.RoutingKey),
            _                                       => QueueAddress(config) // None(默认交换机) 与 Headers(无法用 topic 复刻头部匹配)
        };
    }

    /// <summary>
    /// 计算消费端建立绑定时使用的落点。Routing/Topics 使用队列自身的绑定模式(可含通配符),从而复刻原交换机的分发语义
    /// </summary>
    /// <param name="config">事件配置</param>
    /// <param name="mode">地址解析模式</param>
    public static DelayDestination ResolveBinding(EventConfiguration config, EDelayAddressMode mode)
    {
        if (!string.IsNullOrWhiteSpace(config.DelayAddress))
        {
            return new(EDelayBindingKind.Queue, config.Queue.Name, Normalize(config.DelayAddress));
        }
        if (mode is EDelayAddressMode.QueueDirect)
        {
            return new(EDelayBindingKind.Queue, config.Queue.Name, QueueAddress(config));
        }
        return config.Exchange.Type switch
        {
            EModel.PublishSubscribe         => new(EDelayBindingKind.Exchange, config.Exchange.Name, ExchangeAddress(config)),
            EModel.Routing or EModel.Topics => new(EDelayBindingKind.Queue, config.Queue.Name, RoutingAddress(config, config.Exchange.RoutingKey)),
            _                               => new(EDelayBindingKind.Queue, config.Queue.Name, QueueAddress(config))
        };
    }

    /// <summary>
    /// 判断地址中是否含有会被投递交换机当作通配符解释的字符。
    /// Topics 模式下这是期望行为,其余模式下应提示用户
    /// </summary>
    /// <param name="address">延迟地址</param>
    public static bool ContainsWildcard(string address) => address.Contains('*', StringComparison.Ordinal) || address.Contains('#', StringComparison.Ordinal);

    private static string QueueAddress(EventConfiguration config) => $"q.{Normalize(config.Queue.Name, config.EventType.Name)}";

    private static string ExchangeAddress(EventConfiguration config) => $"x.{Normalize(config.Exchange.Name, DefaultExchangeToken)}";

    private static string RoutingAddress(EventConfiguration config, string routingKey)
    {
        var exchange = Normalize(config.Exchange.Name, DefaultExchangeToken);
        var key = Normalize(routingKey, config.Queue.Name);
        return $"e.{exchange}.{key}";
    }

    // 空白字符在 AMQP 路由键中合法但极易造成误配,统一折叠为 '_';空值回退到给定的兜底名称
    private static string Normalize(string? value, string fallback = "unnamed")
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value;
        return text.Contains(' ', StringComparison.Ordinal) ? text.Replace(' ', '_') : text;
    }
}
