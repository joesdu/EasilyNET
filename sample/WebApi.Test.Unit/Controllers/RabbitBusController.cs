using EasilyNET.RabbitBus.Core.Abstraction;
using Microsoft.AspNetCore.Mvc;
using WebApi.Test.Unit.Events;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// 消息总线测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]/[action]")]
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
            await ibus.PublishBatch(events);
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

    /// <summary>
    /// 延迟投递:消息在指定秒数之后才对消费者可见.
    /// 底层走 DLX + 队列级 TTL 构成的二进制延迟阶梯,不依赖任何 broker 插件.
    /// </summary>
    /// <param name="seconds">延迟秒数</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost]
    public async Task DelayedOrderTimeout(int seconds = 15, CancellationToken cancellationToken = default)
    {
        await ibus.PublishDelayed(new OrderTimeoutEvent
        {
            OrderId = $"ORDER-{DateTime.Now:HHmmss}"
        }, TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 定时投递:消息不早于指定时刻投递,等价于 DoNotDeliverBefore 语义.
    /// </summary>
    /// <param name="deliverAt">最早投递时间,不传则默认为 1 分钟后</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost]
    public async Task ScheduledOrderTimeout(DateTimeOffset? deliverAt = null, CancellationToken cancellationToken = default)
    {
        await ibus.PublishAt(new OrderTimeoutEvent
        {
            OrderId = $"ORDER-{DateTime.Now:HHmmss}"
        }, deliverAt ?? DateTimeOffset.Now.AddMinutes(1), cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 批量延迟投递:一次发送多条共享同一延迟时长的消息.
    /// </summary>
    /// <param name="seconds">延迟秒数</param>
    /// <param name="count">消息条数</param>
    /// <param name="cancellationToken">取消令牌</param>
    [HttpPost]
    public async Task DelayedBatch(int seconds = 30, int count = 5, CancellationToken cancellationToken = default)
    {
        var events = Enumerable.Range(0, count).Select(x => new OrderTimeoutEvent
        {
            OrderId = $"BATCH-{x}"
        });
        await ibus.PublishDelayedBatch(events, TimeSpan.FromSeconds(seconds), cancellationToken: cancellationToken);
    }
}