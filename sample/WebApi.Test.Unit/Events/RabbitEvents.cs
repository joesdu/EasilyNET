using EasilyNET.Core.System;
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Abstraction;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace WebApi.Test.Unit.Events;

/// <summary>
/// 测试HelloWorld模式消息类型
/// </summary>
//[Exchange(EModel.None, queue: "hello.world")]
[Exchange(EModel.Delayed, "xdl.hello", queue: "xdl.hello.world", isDlx: true), QueueArg("x-message-ttl", 5000)]
public class HelloWorldEvent : IEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string Summary { get; set; } = "hello_world";

    /// <summary>
    /// 消息ID
    /// </summary>
    public string EventId => SnowId.GenerateNewId().ToString();
}

/// <summary>
/// 测试WorkQueues模式消息类型
/// </summary>
[Exchange(EModel.None, queue: "work.queue")]
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
[Exchange(EModel.PublishSubscribe, "fanout_exchange", queue: "fanout_queue1")]
public class FanoutEventOne : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "fanout_queue1";
}

/// <summary>
/// 测试发布/订阅(Publish)模式消息类型
/// </summary>
[Exchange(EModel.PublishSubscribe, "fanout_exchange", queue: "fanout_queue2")]
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
[Exchange(EModel.Routing, "direct_exchange", "direct.queue1", "direct_queue1")]
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
[Exchange(EModel.Routing, "direct_exchange", "direct.queue2", "direct_queue2")]
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
[Exchange(EModel.Topics, "topic_exchange", "topic.queue.*", "topic_queue1")]
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
[Exchange(EModel.Topics, "topic_exchange", "topic.queue.1", "topic_queue2")]
// ReSharper disable once ClassNeverInstantiated.Global
public class TopicEventTwo : Event
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue2";
}

#endregion