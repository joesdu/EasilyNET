using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Attributes;
using EasilyNET.RabbitBus.Core.Enums;

namespace WebApi.Test.Unit.Events;

/// <summary>
/// 测试HelloWorld模式消息类型
/// </summary>
[Rabbit(EWorkModel.HelloWorld, queue: "hello.world")]
public class HelloWorldEvent : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string Summary { get; set; } = "hello_world";
}

/// <summary>
/// 测试WorkQueues模式消息类型
/// </summary>
[Rabbit(EWorkModel.WorkQueues, queue: "work.queue")]
public class WorkQueuesEvent : IntegrationEvent
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
[Rabbit(EWorkModel.PublishSubscribe, "fanout_exchange", queue: "fanout_queue1")]
public class FanoutEventOne : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "fanout_queue1";
}

/// <summary>
/// 测试发布/订阅(Publish)模式消息类型
/// </summary>
[Rabbit(EWorkModel.PublishSubscribe, "fanout_exchange", queue: "fanout_queue2")]
// ReSharper disable once ClassNeverInstantiated.Global
public class FanoutEventTwo : IntegrationEvent
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
[Rabbit(EWorkModel.Routing, "direct_exchange", "direct.queue1", "direct_queue1")]
public class DirectEventOne : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "direct_queue1";
}

/// <summary>
/// 测试路由(Routing)模式消息类型
/// </summary>
[Rabbit(EWorkModel.Routing, "direct_exchange", "direct.queue2", "direct_queue2")]
// ReSharper disable once ClassNeverInstantiated.Global
public class DirectEventTwo : IntegrationEvent
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
[Rabbit(EWorkModel.Topics, "topic_exchange", "topic.queue.*", "topic_queue1")]
public class TopicEventOne : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue1";
}

/// <summary>
/// 测试主题(Topic)模式消息类型
/// </summary>
[Rabbit(EWorkModel.Topics, "topic_exchange", "topic.queue.1", "topic_queue2")]
// ReSharper disable once ClassNeverInstantiated.Global
public class TopicEventTwo : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue2";
}

#endregion

/// <summary>
/// 测试RPC模式消息类型
/// </summary>
[Rabbit(EWorkModel.RPC, "topic.queue.*", "topic_queue1")]
public class RPCEvent : IntegrationEvent
{
    /// <summary>
    /// 摘要
    /// </summary>
    public string? Summary { get; set; } = "topic_queue1";
}