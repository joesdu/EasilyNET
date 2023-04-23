namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 自定义事件继承接口
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件ID,雪花ID算法
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    DateTime EventCreateDate { get; }
}