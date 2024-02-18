using EasilyNET.Core.BaseType;

namespace EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
/// 事件基本对象接口,所有的事件均需要继承
/// <see cref="Event" />
/// </summary>
public interface IEvent
{
    /// <summary>
    /// 事件ID,使用雪花ID算法
    /// <see cref="SnowId" />
    /// </summary>
    string EventId { get; }
}