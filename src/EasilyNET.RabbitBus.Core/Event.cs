using System.Text.Json.Serialization;
using EasilyNET.Core.System;
using EasilyNET.RabbitBus.Core.Abstraction;

namespace EasilyNET.RabbitBus.Core;

/// <summary>
/// 基本消息对象,包含事件ID,也可使用IEvent自定义消息结构
/// </summary>
public class Event : IEvent
{
    /// <summary>
    /// 事件Id
    /// </summary>
    [JsonInclude]
    public string EventId { get; } = SnowId.GenerateNewId().ToString();
}