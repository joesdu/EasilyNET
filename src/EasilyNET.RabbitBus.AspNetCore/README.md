#### EasilyNET.RabbitBus.AspNetCore

支持延时队列,服务端需要启用 rabbitmq-delayed-message-exchange 插件

- 支持同一个消息被多个 Handler 消费
- 支持忽略指定 Handler 执行
- 支持事件级 QoS、Headers、交换机/队列参数、优先级队列
- 支持处理器并发控制或顺序执行,内置重试与超时弹性策略
- 现代配置: 使用流畅 API 完成所有配置,无需在事件或处理器上标注特性

##### 如何使用

- 使用 NuGet 包管理工具添加依赖 EasilyNET.RabbitBus.AspNetCore
- 等待下载完成和同意开源协议后,即可使用本库.

###### Step 1. 在 Program.cs 中注册消息总线(现代流式配置)

```csharp
// Program.cs / Startup
builder.Services.AddRabbitBus(c =>
{
    // 1) 连接配置(支持单点/集群,此处以连接串为例)
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!));

    // 2) 消费者默认设置
    c.WithConsumerSettings(dispatchConcurrency: 10, prefetchCount: 100);

    // 3) 弹性与发布确认
    c.WithResilience(retryCount: 5, publisherConfirms: true);

    // 4) 应用标识(可选)
    c.WithApplication("YourAppName");

    // 5) 处理器并发(全局,非顺序执行时生效)
    c.WithHandlerConcurrency(handlerMaxDegreeOfParallelism: 4);

    // ===== 事件配置 =====
    // 5.1 Routing(直连)示例
    c.AddEvent<TestEvent>(EModel.Routing, exchangeName: "test.exchange", routingKey: "test.key", queueName: "test.queue")
     .WithEventQos(prefetchCount: 20)
     .WithEventHeaders(new() { ["x-version"] = "v1" })
     .WithEventQueueArgs(new() { ["x-max-priority"] = 9 }) // 使用优先级需声明此参数
     .WithEventExchangeArgs(new() { ["alternate-exchange"] = "alt.exchange" })
     .ConfigureEvent(ec => ec.SequentialHandlerExecution = false) // 同一消息多个处理器时并行处理
     .And();

    // 5.2 延迟消息示例(需要安装 rabbitmq-delayed-message-exchange 插件)
    c.AddEvent<DelayedMessageEvent>(EModel.Delayed, exchangeName: "delayed.exchange", routingKey: "delayed.key", queueName: "delayed.queue");

    // 5.3 发布/订阅(Fanout)
    c.AddEvent<FanoutEvent>(EModel.PublishSubscribe, "fanout.exchange", queueName: "fanout.queue");

    // 忽略某个处理器(当同一事件存在多个处理器时)
    c.IgnoreHandler<TestEvent, TestEventHandlerSecond>();

    // 如需自定义序列化器
    // c.WithSerializer<MsgPackSerializer>();
});
```

###### Step 2. 定义事件与处理器(无需再使用特性)

```csharp
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Abstraction;

// 事件只需实现 IEvent 或继承 Event,无需 [Exchange]/[Qos]/[IgnoreHandler] 等特性
public class TestEvent : Event
{
    public string Message { get; set; } = default!;
}

public class DelayedMessageEvent : Event
{
    public string Message { get; set; } = default!;
}

public class TestEventHandler(ILogger<TestEventHandler> logger) : IEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event)
    {
        logger.LogInformation("TestEvent: {event} @ {time}", @event.Message, DateTime.Now);
        return Task.CompletedTask;
    }
}

// 仍可定义多个处理器消费同一消息,是否忽略通过配置 c.IgnoreHandler<TEvent, THandler>() 控制
public class TestEventHandlerSecond(ILogger<TestEventHandlerSecond> logger) : IEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent @event)
    {
        logger.LogInformation("SecondHandler: {event} @ {time}", @event.Message, DateTime.Now);
        return Task.CompletedTask;
    }
}
```

###### Step 3. 发布消息

```csharp
// 构造注入 IBus 后使用
private readonly IBus _bus;

public MyController(IBus bus) => _bus = bus;

[HttpPost("send")]
public async Task Send()
{
    // 普通消息(按事件配置路由)
    await _bus.Publish(new TestEvent { Message = "normal" });

    // 显式指定 routingKey, 适配 Topic/多路由发信场景
    await _bus.Publish(new TestEvent { Message = "topic" }, routingKey: "topic.queue.1");

    // 使用优先级(需为队列设置 x-max-priority)
    await _bus.Publish(new TestEvent { Message = "priority" }, priority: 5);

    // 延迟消息(毫秒)
    await _bus.Publish(new DelayedMessageEvent { Message = "delay-5s" }, ttl: 5000);
}
```

#### 使用自定义序列化器

- 默认序列化器为 System.Text.Json
- 若要使用其他序列化器,实现 IBusSerializer 接口并在注册时使用 WithSerializer<T>() 指定

```csharp
public sealed class MsgPackSerializer : IBusSerializer
{
    private static readonly MessagePackSerializerOptions standardOptions =
        MessagePackSerializerOptions.Standard
            .WithResolver(CompositeResolver.Create(NativeDateTimeResolver.Instance,
                ContractlessStandardResolver.Instance))
            .WithSecurity(MessagePackSecurity.UntrustedData);

    private static readonly MessagePackSerializerOptions lz4BlockArrayOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4BlockArray);

    private static readonly MessagePackSerializerOptions lz4BlockOptions =
        standardOptions.WithCompression(MessagePackCompression.Lz4Block);

    public byte[] Serialize(object? obj, Type type)
    {
        var data = MessagePackSerializer.Serialize(type, obj, standardOptions);
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Serialize(type, obj, options);
    }

    public object? Deserialize(byte[] data, Type type)
    {
        var options = data.Length > 8192 ? lz4BlockArrayOptions : lz4BlockOptions;
        return MessagePackSerializer.Deserialize(type, data, options);
    }
}

// 注册
builder.Services.AddRabbitBus(c =>
{
    // ...其他配置
    c.WithSerializer<MsgPackSerializer>();
});
```

#### 注意事项

- 延迟队列必须安装 rabbitmq-delayed-message-exchange 插件; 框架会自动为延迟交换机声明 x-delayed-type=direct
- 若使用优先级,请为队列设置参数 x-max-priority
- EModel.None 表示不显式声明交换机,使用默认交换机; 此时 routingKey 应为队列名
- Publish 的 routingKey 参数用于覆盖事件配置中的路由键,便于 Topic 下多路由生产者发信
- 可通过 ConfigureEvent(ec => ec.SequentialHandlerExecution = true) 开启同一事件多个处理器的顺序执行
