#### EasilyNET.RabbitBus.AspNetCore

##### 如何使用

- 首先使用 Nuget 包管理工具添加依赖 EasilyNET.RabbitBus.AspNetCore
- 等待下载完成和同意开源协议后,即可使用本库.
- Step1.在 Program.cs 中配置消息总线

```csharp
// 配置服务(亦可使用集群模式或者使用配置文件)
builder.Services.AddRabbitBus(c =>
{
    c.Host = "192.168.2.110";
    c.Port = 5672;
    c.UserName = "username";
    c.PassWord = "password";
    ...
});

// 注册服务
builder.Services.AddTransient<TestEventHandler>();
```

- Step2.接下来配置事件和事件处理器

```csharp
/// <summary>
/// 测试消息类型
/// </summary>
[Rabbit("hoyo.test", EExchange.Routing, "test", "orderqueue2")]
public class TestEvent : IntegrationEvent
{
    /// <summary>
    /// 消息
    /// </summary>
    public string Message { get; set; } = default!;
}

/// <summary>
/// 消息处理Handler
/// </summary>
public class TestEventHandler : IIntegrationEventHandler<TestEvent>
{
    private readonly ILogger<TestEventHandler> _logger;
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger"></param>
    public TestEventHandler(ILogger<TestEventHandler> logger)
    {
        _logger = logger;
    }
    /// <summary>
    /// 当消息到达的时候执行的Action
    /// </summary>
    /// <param name="event"></param>
    /// <returns></returns>
    public Task HandleAsync(TestEvent @event)
    {
        _logger.LogInformation("TestEvent_{event}-----{date}", @event.Message, DateTime.Now);
        return Task.CompletedTask;
    }
}
```

- Step3.使用消息队列发送消息

```csharp
private readonly IIntegrationEventBus _ibus;
// 控制器构造函数伪代码
construct(IIntegrationEventBus ibus){
   _ibus = ibus;
}
/// <summary>
/// 创建一个延时消息,同时发送一个普通消息做对比
/// </summary>
[HttpPost("TTLTest")]
public void TTLTest()
{
    var rand = new Random();
    var ttl = rand.Next(1000, 10000);
    var ttlobj = new DelayedMessageEvent() { Message = $"延迟{ttl}毫秒,当前时间{DateTime.Now:yyyy-MM-dd HH:mm:ss}" };
    // 延时队列需要服务端安装延时队列插件.
    _ibus.Publish(ttlobj, (uint)ttl);
    _ibus.Publish(ttlobj);
}
```
