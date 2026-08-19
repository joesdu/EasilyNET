using EasilyNET.RabbitBus.Delayed;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.RabbitBus.Configs;

/// <summary>
///     <para xml:lang="en">
///     Delayed delivery configuration. The implementation uses a binary delay ladder built from ordinary topic exchanges, queue level
///     TTL and dead-lettering, so it works on any vanilla RabbitMQ (including quorum queues and clusters) and does not depend on the
///     discontinued <c>rabbitmq-delayed-message-exchange</c> plugin
///     </para>
///     <para xml:lang="zh">
///     延迟投递配置。实现基于由普通 topic 交换机、队列级 TTL 与死信转发构成的二进制延迟阶梯,
///     因此可运行在任何原生 RabbitMQ 上(含仲裁队列与集群),不依赖已停止维护的
///     <c>rabbitmq-delayed-message-exchange</c> 插件
///     </para>
/// </summary>
public sealed class DelayedDeliveryConfig
{
    private int _levelCount = DelayLadder.MaxSupportedLevelCount;

    /// <summary>
    ///     <para xml:lang="en">Whether delayed delivery is enabled. Default is false</para>
    ///     <para xml:lang="zh">是否启用延迟投递。默认是 false</para>
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Name prefix of the ladder topology. Levels are named <c>{Prefix}-level-NN</c>, the delivery exchange <c>{Prefix}-delivery</c>.
    ///     Changing it after the topology exists creates a second, independent ladder
    ///     </para>
    ///     <para xml:lang="zh">
    ///     阶梯拓扑的名称前缀。档位命名为 <c>{Prefix}-level-NN</c>,投递交换机为 <c>{Prefix}-delivery</c>。
    ///     拓扑已存在后修改该值会创建另一套彼此独立的阶梯
    ///     </para>
    /// </summary>
    public string Prefix { get; set; } = "easilynet.v1.delay";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Number of ladder levels (1-28). It determines both the maximum delay (<c>2^LevelCount-1</c> seconds) and the width of the
    ///     ladder routing key, so publishers and consumers sharing a ladder MUST use the same value. Default is 28
    ///     </para>
    ///     <para xml:lang="zh">
    ///     阶梯档位数量(1-28)。它同时决定最大延迟(<c>2^LevelCount-1</c> 秒)与阶梯路由键的宽度,
    ///     因此共用同一套阶梯的生产者与消费者必须使用相同的值。默认是 28
    ///     </para>
    /// </summary>
    public int LevelCount
    {
        get => _levelCount;
        set => _levelCount = DelayLadder.ClampLevelCount(value);
    }

    /// <summary>
    ///     <para xml:lang="en">How the delivery destination of a delayed message is resolved. Default is <see cref="EDelayAddressMode.RoutingAware" /></para>
    ///     <para xml:lang="zh">延迟消息投递目标的解析方式。默认是 <see cref="EDelayAddressMode.RoutingAware" /></para>
    /// </summary>
    public EDelayAddressMode AddressMode { get; set; } = EDelayAddressMode.RoutingAware;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether the ladder queues are declared as quorum queues (replicated, at-least-once dead-lettering). Set to false to use
    ///     classic lazy queues on single node brokers. Default is true
    ///     </para>
    ///     <para xml:lang="zh">
    ///     阶梯队列是否声明为仲裁队列(多副本、at-least-once 死信策略)。单节点环境可设为 false 以使用经典惰性队列。默认是 true
    ///     </para>
    /// </summary>
    public bool UseQuorumQueues { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Extra arguments merged into every ladder queue declaration (for example <c>x-max-length</c>)</para>
    ///     <para xml:lang="zh">合并到每个阶梯队列声明中的额外参数(例如 <c>x-max-length</c>)</para>
    /// </summary>
    public Dictionary<string, object?> QueueArguments { get; } = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether the ladder topology is declared automatically on startup and after a reconnect. Set to false when the topology is
    ///     provisioned externally (definitions file, IaC). Default is true
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否在启动及重连后自动声明阶梯拓扑。当拓扑由外部方式(definitions 文件、IaC)预置时可设为 false。默认是 true
    ///     </para>
    /// </summary>
    public bool AutoDeclareTopology { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">Highest ladder level index</para>
    ///     <para xml:lang="zh">最高档位下标</para>
    /// </summary>
    public int MaxLevel => LevelCount - 1;

    /// <summary>
    ///     <para xml:lang="en">Maximum delay this ladder can express</para>
    ///     <para xml:lang="zh">该阶梯可表达的最大延迟</para>
    /// </summary>
    public TimeSpan MaxDelay => TimeSpan.FromSeconds(DelayLadder.MaxDelaySeconds(LevelCount));

    /// <summary>
    ///     <para xml:lang="en">Name of the delivery exchange, the last hop before the destination</para>
    ///     <para xml:lang="zh">投递交换机名称,即抵达目标前的最后一跳</para>
    /// </summary>
    public string DeliveryExchange => $"{Prefix}-delivery";

    /// <summary>
    ///     <para xml:lang="en">Name of the exchange and queue of a ladder level</para>
    ///     <para xml:lang="zh">某个阶梯档位的交换机与队列名称</para>
    /// </summary>
    /// <param name="level">
    ///     <para xml:lang="en">Level index</para>
    ///     <para xml:lang="zh">档位下标</para>
    /// </param>
    public string LevelName(int level) => $"{Prefix}-level-{level:D2}";
}
