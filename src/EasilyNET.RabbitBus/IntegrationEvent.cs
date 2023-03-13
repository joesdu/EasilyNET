using EasilyNET.RabbitBus.Abstractions;
using System.Text.Json.Serialization;

namespace EasilyNET.RabbitBus;

/// <summary>
/// 事件基本对象,所有的事件均需要继承此类
/// </summary>
public class IntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// 事件ID
    /// </summary>
    [JsonInclude]
    public string EventId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 事件创建时间
    /// </summary>
    [JsonInclude]
    public DateTime EventCreateDate { get; } = DateTime.Now;
}