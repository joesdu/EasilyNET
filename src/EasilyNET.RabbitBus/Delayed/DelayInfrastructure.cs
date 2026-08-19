using EasilyNET.RabbitBus.Configs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
/// 一次延迟发布所需的全部信息:入口档位交换机、阶梯路由键、延迟地址与期望投递时间
/// </summary>
internal readonly record struct DelayPublishPlan(string LevelExchange, string RoutingKey, string Address, int DelaySeconds, DateTime DeliverAtUtc);

/// <summary>
/// 延迟阶梯拓扑的声明与绑定。
/// 拓扑形如:
/// <code>
/// 发布 -> level-N(topic) --位为1--> level-N队列(TTL=2^N秒) --DLX--> level-(N-1)(topic) -> ... -> delivery(topic) -> 目标
///                        --位为0--> level-(N-1)(topic) 直接透传
/// </code>
/// 每个档位队列内的消息 TTL 完全相同,因此不存在队头阻塞;跳过的档位不产生任何存储开销。
/// </summary>
internal sealed class DelayInfrastructure(IOptionsMonitor<RabbitConfig> options, ILogger<DelayInfrastructure> logger)
{
    /// <summary>
    /// 当前延迟投递配置
    /// </summary>
    public DelayedDeliveryConfig Config => options.Get(Constant.OptionName).DelayedDelivery;

    /// <summary>
    /// 是否启用了延迟投递
    /// </summary>
    public bool Enabled => Config.Enabled;

    /// <summary>
    /// 为一次延迟发布计算发布计划
    /// </summary>
    /// <param name="eventConfig">事件配置</param>
    /// <param name="routingKey">本次发布显式指定的路由键</param>
    /// <param name="delay">延迟时长</param>
    public DelayPublishPlan CreatePlan(EventConfiguration eventConfig, string? routingKey, TimeSpan delay)
    {
        var cfg = Config;
        var seconds = DelayLadder.ToDelaySeconds(delay);
        var max = DelayLadder.MaxDelaySeconds(cfg.LevelCount);
        if (seconds > max)
        {
            throw new ArgumentOutOfRangeException(nameof(delay), delay,
                $"The delay exceeds the configured maximum of {cfg.MaxDelay}. Increase the ladder level count via WithDelayedDelivery(maxDelay: ...), or schedule the message with an external scheduler.");
        }
        var address = DelayAddressResolver.ResolvePublishAddress(eventConfig, routingKey, cfg.AddressMode);
        var key = DelayLadder.CalculateRoutingKey(seconds, address, cfg.LevelCount, out var startingLevel);
        return new(cfg.LevelName(startingLevel), key, address, seconds, DateTime.UtcNow.AddSeconds(seconds));
    }

    /// <summary>
    /// 声明整套阶梯拓扑。声明是幂等的,可在启动与每次重连后重复调用
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="ct">取消令牌</param>
    public async Task DeclareTopologyAsync(IChannel channel, CancellationToken ct)
    {
        var cfg = Config;
        if (!cfg.Enabled || !cfg.AutoDeclareTopology)
        {
            return;
        }
        var maxLevel = cfg.MaxLevel;

        // 1. 档位交换机 + 档位队列 + "该位为 1 则入队" 的绑定
        for (var level = maxLevel; level >= 0; level--)
        {
            var current = cfg.LevelName(level);
            await channel.ExchangeDeclareAsync(current, ExchangeType.Topic, true, false, null, cancellationToken: ct).ConfigureAwait(false);
            await channel.QueueDeclareAsync(current, true, false, false, BuildQueueArguments(cfg, level), cancellationToken: ct).ConfigureAwait(false);
            await channel.QueueBindAsync(current, current, DelayLadder.QueueBindingKey(level, cfg.LevelCount), cancellationToken: ct).ConfigureAwait(false);
        }

        // 2. 投递交换机
        await channel.ExchangeDeclareAsync(cfg.DeliveryExchange, ExchangeType.Topic, true, false, null, cancellationToken: ct).ConfigureAwait(false);

        // 3. "该位为 0 则跳过本档位" 的交换机到交换机透传链，最低档位透传到投递交换机
        for (var level = maxLevel; level >= 1; level--)
        {
            await channel.ExchangeBindAsync(cfg.LevelName(level - 1), cfg.LevelName(level), DelayLadder.PassThroughBindingKey(level, cfg.LevelCount), cancellationToken: ct).ConfigureAwait(false);
        }
        await channel.ExchangeBindAsync(cfg.DeliveryExchange, cfg.LevelName(0), DelayLadder.PassThroughBindingKey(0, cfg.LevelCount), cancellationToken: ct).ConfigureAwait(false);
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Delay ladder topology declared: {Prefix}, {LevelCount} levels, max delay {MaxDelay}", cfg.Prefix, cfg.LevelCount, cfg.MaxDelay);
        }
    }

    /// <summary>
    /// 仅声明投递交换机。消费端建立绑定前调用,避免为此重复声明整套阶梯
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="ct">取消令牌</param>
    public async Task DeclareDeliveryExchangeAsync(IChannel channel, CancellationToken ct)
    {
        var cfg = Config;
        if (!cfg.Enabled || !cfg.AutoDeclareTopology)
        {
            return;
        }
        await channel.ExchangeDeclareAsync(cfg.DeliveryExchange, ExchangeType.Topic, true, false, null, cancellationToken: ct).ConfigureAwait(false);
    }

    /// <summary>
    /// 将落点绑定到投递交换机
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="destination">落点</param>
    /// <param name="ct">取消令牌</param>
    public async Task BindDestinationAsync(IChannel channel, DelayDestination destination, CancellationToken ct)
    {
        var cfg = Config;
        if (!cfg.Enabled || string.IsNullOrWhiteSpace(destination.Name) || string.IsNullOrWhiteSpace(destination.Address))
        {
            return;
        }
        var key = DelayLadder.BindingKey(destination.Address);
        if (destination.Kind is EDelayBindingKind.Queue)
        {
            await channel.QueueBindAsync(destination.Name, cfg.DeliveryExchange, key, cancellationToken: ct).ConfigureAwait(false);
        }
        else
        {
            await channel.ExchangeBindAsync(destination.Name, cfg.DeliveryExchange, key, cancellationToken: ct).ConfigureAwait(false);
        }
        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("Bound delay destination {Kind} '{Name}' to {DeliveryExchange} with '{BindingKey}'", destination.Kind, destination.Name, cfg.DeliveryExchange, key);
        }
    }

    private static Dictionary<string, object?> BuildQueueArguments(DelayedDeliveryConfig cfg, int level)
    {
        var args = new Dictionary<string, object?>
        {
            // 队列级 TTL:该档位内所有消息的过期时间完全一致
            ["x-message-ttl"] = (1L << level) * 1000L,
            // 过期后死信到下一个更低的档位,档位 0 过期后直接进入投递交换机
            ["x-dead-letter-exchange"] = level > 0 ? cfg.LevelName(level - 1) : cfg.DeliveryExchange
        };
        if (cfg.UseQuorumQueues)
        {
            args["x-queue-type"] = "quorum";
            // at-least-once 需要配合 reject-publish,否则 RabbitMQ 会拒绝该组合
            args["x-dead-letter-strategy"] = "at-least-once";
            args["x-overflow"] = "reject-publish";
        }
        // 经典队列不额外设参数:x-queue-mode=lazy 自 RabbitMQ 3.12 起已被弃用并忽略
        foreach (var (k, v) in cfg.QueueArguments)
        {
            args[k] = v;
        }
        return args;
    }
}
