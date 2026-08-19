namespace EasilyNET.RabbitBus.Delayed;

/// <summary>
/// 延迟消息离开投递交换机后的落点类型
/// </summary>
internal enum EDelayBindingKind
{
    /// <summary>
    /// 绑定到队列,消息直接进入该队列
    /// </summary>
    Queue,

    /// <summary>
    /// 交换机到交换机绑定,消息重新进入目标交换机并按其自身规则分发
    /// </summary>
    Exchange
}

/// <summary>
/// 延迟消息的落点:绑定类型、被绑定对象名称,以及用于匹配的延迟地址
/// </summary>
internal readonly record struct DelayDestination(EDelayBindingKind Kind, string Name, string Address);
