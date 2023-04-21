using EasilyNET.Core.Language;
using EasilyNET.RabbitBus.Core;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController, Route("api/[controller]/[action]")]
public class RabbitBusController : ControllerBase
{
    private readonly IIntegrationEventBus _ibus;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="ibus"></param>
    public RabbitBusController(IIntegrationEventBus ibus)
    {
        _ibus = ibus;
    }

    /// <summary>
    /// 发送HelloWorld消息
    /// </summary>
    [HttpPost]
    public void HelloWorld()
    {
        var rand = new Random();
        _ibus.Publish(new HelloWorldEvent(), priority: (byte)rand.Next(0, 9));
    }

    /// <summary>
    /// 发送WorkQueues消息
    /// </summary>
    [HttpPost]
    public void WorkQueues()
    {
        foreach (var i in ..10)
        {
            _ibus.Publish(new WorkQueuesEvent
            {
                Summary = $"WorkQueuesEvent:{i}"
            });
        }
    }

    /// <summary>
    /// Fanout(发布订阅)发送消息,设置两个队列,所以应该输出两条信息
    /// </summary>
    [HttpPost]
    public void Fanout()
    {
        _ibus.Publish(new FanoutEventOne());
    }

    /// <summary>
    /// 路由模式(direct)模式发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public void DirectQueue1()
    {
        _ibus.Publish(new DirectEventOne(), "direct.queue1");
    }

    /// <summary>
    /// 路由模式(direct)发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public void DirectQueue2()
    {
        _ibus.Publish(new DirectEventOne(), "direct.queue2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.1]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出两条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo1()
    {
        _ibus.Publish(new TopicEventOne(), "topic.queue.1");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.2]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo2()
    {
        _ibus.Publish(new TopicEventOne(), "topic.queue.2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.3]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public void TopicTo3()
    {
        _ibus.Publish(new TopicEventOne(), "topic.queue.3");
    }
}