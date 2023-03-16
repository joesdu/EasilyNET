namespace EasilyNET.RabbitBus.Abstractions;

/// <summary>
/// 自定义事件继承接口
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// 事件创建时间
    /// </summary>
    // ReSharper disable once UnusedMemberInSuper.Global
    DateTime EventCreateDate { get; }
}