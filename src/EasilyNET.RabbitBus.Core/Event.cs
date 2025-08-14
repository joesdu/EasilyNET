using System.Text.Json.Serialization;
using EasilyNET.Core.Essentials;
using EasilyNET.RabbitBus.Core.Abstraction;

namespace EasilyNET.RabbitBus.Core;

/// <summary>
///     <para xml:lang="en">Basic message object, includes event ID. You can also use IEvent to customize the message structure</para>
///     <para xml:lang="zh">基本消息对象,包含事件ID,也可使用IEvent自定义消息结构</para>
/// </summary>
public class Event : IEvent
{
    /// <summary>
    ///     <para xml:lang="en">Event ID</para>
    ///     <para xml:lang="zh">事件Id</para>
    /// </summary>
    [JsonInclude]
    public string EventId { get; } = ObjectIdCompat.GenerateNewId().ToString();
}