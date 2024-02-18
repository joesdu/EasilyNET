using EasilyNET.Core.BaseType;
using EasilyNET.RabbitBus.Core.Abstraction;
using System.Text.Json.Serialization;

namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 基本消息对象,包含事件ID和创建时间,也可使用IEvent自定义消息结构
/// </summary>
public class Event : IEvent
{
    /// <summary>
    /// 事件Id
    /// </summary>
    [JsonInclude]
    public string EventId { get; } = SnowId.GenerateNewId().ToString();
}