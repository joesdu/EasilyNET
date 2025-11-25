using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[ApiExplorerSettings(GroupName = "RabbitBus")]
public class RabbitBusController(IBus ibus) : ControllerBase
{
    /// <summary>
    /// 发送HelloWorld消息
    /// </summary>
    [HttpPost]
    public async Task HelloWorld()
    {
        var rand = new Random();
        await ibus.Publish(new HelloWorldEvent(), priority: (byte)rand.Next(0, 9));
    }

    /// <summary>
    /// 发送HelloWorld消息,使用延时插件
    /// </summary>
    [HttpPost]
    public async Task DeadLetter()
    {
        var rand = new Random();
        await ibus.Publish(new HelloWorldEvent(), 3000, priority: (byte)rand.Next(0, 9));
    }

    /// <summary>
    /// 发送WorkQueues消息
    /// </summary>
    [HttpPost]
    public async Task WorkQueues()
    {
        await Task.Factory.StartNew(async () =>
        {
            var events = Enumerable.Range(0, 30).Select(x => new WorkQueuesEvent
            {
                Summary = $"WorkQueuesEvent:{x}"
            }).ToList();
            await ibus.PublishBatch(events, multiThread: false);
        });
    }

    /// <summary>
    /// Fanout(发布订阅)发送消息,设置两个队列,所以应该输出两条信息
    /// </summary>
    [HttpPost]
    public async Task Fanout(CancellationToken cancellationToken) => await ibus.Publish(new FanoutEventOne(), cancellationToken: cancellationToken);

    /// <summary>
    /// 路由模式(direct)模式发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public async Task DirectQueue1(CancellationToken cancellationToken)
    {
        await ibus.Publish(new DirectEventOne(), "direct.queue1", cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 路由模式(direct)发送消息,只向单一主题发送消息
    /// </summary>
    [HttpPost]
    public async Task DirectQueue2()
    {
        await ibus.Publish(new DirectEventTwo(), "direct.queue2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.1]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出两条信息.
    /// </summary>
    [HttpPost]
    public async Task TopicTo1()
    {
        await ibus.Publish(new TopicEventOne(), "topic.queue.1");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.2]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public async Task TopicTo2()
    {
        await ibus.Publish(new TopicEventOne(), "topic.queue.2");
    }

    /// <summary>
    /// Topic(主题模式)发送消息,向订阅了,[topic.queue.3]主题的队列发送消息.
    /// 只配置了topic.queue.*和topic.queue.1,所以该接口应该只输出一条信息.
    /// </summary>
    [HttpPost]
    public async Task TopicTo3()
    {
        await ibus.Publish(new TopicEventOne(), "topic.queue.3");
    }
}