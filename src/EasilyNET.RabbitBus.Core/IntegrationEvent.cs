using EasilyNET.Core.BaseType;
using System.Text.Json.Serialization;

namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 事件基本对象,所有的事件均需要继承此类
/// </summary>
public class IntegrationEvent : IIntegrationEvent
{
    /// <summary>
    /// 事件ID,雪花ID算法
    /// </summary>
    [JsonInclude]
    public string EventId { get; } = SnowId.GenerateNewId().ToString();

    /// <summary>
    /// 事件创建时间
    /// </summary>
    [JsonInclude]
    public DateTime EventCreateDate { get; } = DateTime.Now.ToLocalTime();
}