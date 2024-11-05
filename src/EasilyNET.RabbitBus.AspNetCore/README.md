#### EasilyNET.RabbitBus.AspNetCore

-

支持延时队列,服务端需要启用 [rabbitmq-delayed-message-exchange](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)
插件
- 支持同一个消息被多个 Handler 消费
- 若是就是想写多个 Handler 但是又希望某些 Handler 不执行,可以在不需要的 Handler 上标记 [IgnoreHandler] 特性
- 添加 MessagePack 序列化选项,可通过 ESerializer.MessagePack 或 ESerializer.TextJson 进行切换.

##### 如何使用

- 首先使用 Nuget 包管理工具添加依赖 EasilyNET.RabbitBus.AspNetCore
- 等待下载完成和同意开源协议后,即可使用本库.
- Step1.在 Program.cs 中配置消息总线

```csharp
// 配置服务(亦可使用集群模式或者使用配置文件,或者环境变量.)
builder.Services.AddRabbitBus(c =>
{
    c.Host = "192.168.2.110";
    c.Port = 5672;
    c.UserName = "username";
    c.PassWord = "password";
    c.PoolCount = (uint)Environment.ProcessorCount;
    c.RetryCount = 5;
    c.Serializer = ESerializer.MessagePack;
    ...
});
```

- Step2.接下来配置事件和事件处理器

```csharp
/// <summary>
/// 测试消息类型,消息继承自 IEvent 或者 Event
/// </summary>
[Exchange("hoyo.test", EModel.Routing, "test", "orderqueue2")]
public class TestEvent : Event
{
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = default!;
}

/// <summary>
/// 消息处理Handler
/// </summary>
public class TestEventHandler(ILogger<TestEventHandler> logger) : IEventHandler<TestEvent>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(TestEvent @event)
    {
        logger.LogInformation("TestEvent_{event}-----{date}", @event.Message, DateTime.Now);
        return Task.CompletedTask;
    }
}

/// <summary>
/// 若是存在同一个消息多个 Handler 实现,比如这里我们写了两个 Handler,那么发送一次消息这两个 Handler 均会执行.
/// </summary>
/// 若是不希望这个 Handler 执行可以标记
[IgnoreHandler]
public class TestEventHandlerSecond(ILogger<TestEventHandlerSecond> logger) : IEventHandler<TestEvent>
{
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(TestEvent @event)
    {
        logger.LogInformation("TestEvent_{event}-----{date}", @event.Message, DateTime.Now);
        return Task.CompletedTask;
    }
}
```

- Step3.使用消息队列发送消息

```csharp
private readonly IBus _ibus;
// 控制器构造函数伪代码
construct(IBus ibus){
   _ibus = ibus;
}
/// <summary>
/// 创建一个延时消息,同时发送一个普通消息做对比
/// </summary>
[HttpPost("TTLTest")]
public async Task TTLTest()
{
    var rand = new Random();
    var ttl = rand.Next(1000, 10000);
    var ttlobj = new DelayedMessageEvent() { Message = $"延迟{ttl}毫秒,当前时间{DateTime.Now:yyyy-MM-dd HH:mm:ss}" };
    // 延时队列需要服务端安装延时队列插件.
    await _ibus.Publish(ttlobj, (uint)ttl);
    await _ibus.Publish(ttlobj);
}
```
