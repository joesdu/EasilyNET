using EasilyNET.Core.Enums;
using EasilyNET.Core.Essentials;
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Abstraction;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Events;

/// <summary>
/// 测试HelloWorld模式消息类型
/// </summary>
// 现代配置方式：所有配置都在RabbitModule.cs中通过流畅API配置
// 不再需要 [Exchange], [Queue], [Qos] 等属性
public class HelloWorldEvent : IEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string Summary { get; set; } = "hello_world";

    /// <summary>
    /// 用于测试枚举
    /// </summary>
    public EGender Gender { get; set; } = EGender.男;

    /// <summary>
    /// 消息ID
    /// </summary>
    public string EventId => ObjectIdCompat.GenerateNewId().ToString();
}

/// <summary>
/// 测试WorkQueues模式消息类型
/// </summary>
public class WorkQueuesEvent : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "work_queue";
}

#region Publish模式,Fanout模式会发布到所有队列中,所以这里routingkey没有意义

/// <summary>
/// 测试发布/订阅(Publish)模式消息类型
/// </summary>
public class FanoutEventOne : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "fanout_queue1";

    /// <summary>
    /// 字符串列表
    /// </summary>
    public List<string> StringList { get; set; } = ["one", "two", "three"];
}

/// <summary>
/// 测试发布/订阅(Publish)模式消息类型
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class FanoutEventTwo : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "fanout_queue2";
}

#endregion

#region Routing模式

/// <summary>
/// 测试路由(Routing)模式消息类型
/// </summary>
public class DirectEventOne : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "direct_queue1";
}

/// <summary>
/// 测试路由(Routing)模式消息类型
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class DirectEventTwo : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "direct_queue2";
}

#endregion

#region Topic模式

/// <summary>
/// 测试主题(Topic)模式消息类型
/// </summary>
public class TopicEventOne : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue1";
}

/// <summary>
/// 测试主题(Topic)模式消息类型
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class TopicEventTwo : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue2";
}

#endregion