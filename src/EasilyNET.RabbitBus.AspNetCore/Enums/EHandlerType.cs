namespace EasilyNET.RabbitBus.AspNetCore.Enums;

/// <summary>
///     <para xml:lang="en">Types of message handlers</para>
///     <para xml:lang="zh">消息处理程序的类型</para>
/// </summary>
internal enum EKindOfHandler
{
    /// <summary>
    ///     <para xml:lang="en">Normal message handler</para>
    ///     <para xml:lang="zh">普通消息处理程序</para>
    /// </summary>
    Normal,

    /// <summary>
    ///     <para xml:lang="en">Delayed message handler</para>
    ///     <para xml:lang="zh">延迟消息处理程序</para>
    /// </summary>
    Delayed
}