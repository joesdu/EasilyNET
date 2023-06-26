using EasilyNET.Core.Language;
using EasilyNET.RabbitBus.Core;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController, Route("api/[controller]/[action]"), ApiGroup("RabbitBus", "v1", "RabbitBus Test")]
public class RabbitBusController(IIntegrationEventBus ibus) : ControllerBase
{
    /// <summary>
    /// 发送HelloWorld消息
    /// </summary>
    [HttpPost]
    public void HelloWorld()
    {
        var rand = new Random();
        ibus.Publish(new HelloWorldEvent(), priority: (byte)rand.Next(0, 9));
    }

    /// <summary>
    /// 发送WorkQueues消息
    /// </summary>
    [HttpPost]
    public void WorkQueues()
    {
        foreach (var i in ..10_0000)
        {
            ibus.Publish(new WorkQueuesEvent
            {
                Summary = $"WorkQueuesEvent:{i}"
            });
        }
    }

    /// <summary>
    /// Fanout(发布订阅)发送消息,设置两个队列,所以应该输出两条信息
    /// </summary>
    [HttpPost]
    public void Fanout(CancellationToken cancellationToken)
    {
        ibus.Publish(new FanoutEventOne(), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 路由模式(direct)模式发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public Task DirectQueue1(CancellationToken cancellationToken)
    {
        Task.Run(() => ibus.Publish(new DirectEventOne(), "direct.queue1", cancellationToken: cancellationToken), cancellationToken);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 路由模式(direct)发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public void DirectQueue2()
    {
        ibus.Publish(new DirectEventOne(), "direct.queue2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.1]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出两条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo1()
    {
        ibus.Publish(new TopicEventOne(), "topic.queue.1");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.2]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo2()
    {
        ibus.Publish(new TopicEventOne(), "topic.queue.2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.3]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo3()
    {
        ibus.Publish(new TopicEventOne(), "topic.queue.3");
    }
}