// ReSharper disable UnusedMemberInSuper.Global

using EasilyNET.Core.BaseType;

namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 事件基本对象接口,所有的事件均需要继承
/// <see cref="IntegrationEvent" />
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件ID,使用雪花ID算法
    /// <see cref="SnowId" />
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    DateTime EventCreateDate { get; }
}