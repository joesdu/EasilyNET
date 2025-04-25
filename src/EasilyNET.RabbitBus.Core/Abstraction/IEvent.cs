using EasilyNET.Core.Essentials;

namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
///     <para xml:lang="en">Basic event object interface, all events need to inherit from this</para>
///     <para xml:lang="zh">事件基本对象接口，所有的事件均需要继承</para>
///     <see cref="Event" />
/// </summary>
public interface IEvent
{
    /// <summary>
    ///     <para xml:lang="en">Event ID, using Snowflake ID algorithm</para>
    ///     <para xml:lang="zh">事件ID，使用雪花ID算法</para>
    ///     <see cref="SnowId" />
    /// </summary>
    string EventId { get; }
}