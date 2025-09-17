#### EasilyNET.RabbitBus.AspNetCore

支持延时队列,服务端需要启用 rabbitmq-delayed-message-exchange 插件

- 支持同一个消息被多个 Handler 消费
- 支持忽略指定 Handler 执行
- 支持事件级 QoS、Headers、交换机/队列参数、优先级队列
- 支持处理器并发控制或顺序执行,内置重试与超时弹性策略
- 支持发布确认(Publisher Confirms)确保消息可靠性投递
- 支持批量发布消息提高吞吐量
- 现代配置: 使用流畅 API 完成所有配置,无需在事件或处理器上标注特性
- 内建发布失败重试(Nack/Confirm 超时)的后台调度器,指数退避 + 抖动
- 发布背压: 启用 PublisherConfirms 时,对最大未确认发布数进行限流(可配置)
- 死信存储: 超过最大重试后写入死信存储(默认内存实现,可替换)
- 健康检查与可观测性: 暴露连接/发布/重试等指标,并提供健康检查

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
    // ConsumerDispatchConcurrency: 消费者调度并发数,控制同时处理的消息数(默认: 10)
    // PrefetchCount: QoS预取计数,限制未确认消息的数量(默认: 100)
    c.WithConsumerSettings(dispatchConcurrency: 10, prefetchCount: 100);

    // 3) 弹性与发布确认
    // PublisherConfirms: 启用发布确认模式,确保消息可靠投递(默认: true)
    // MaxOutstandingConfirms: 最大未确认发布数量(默认: 1000)
    // BatchSize: 批量发布大小(默认: 100)
    // ConfirmTimeoutMs: 发布确认超时时间(毫秒,默认: 30000)
    c.WithResilience(retryCount: 5, publisherConfirms: true, maxOutstandingConfirms: 1000, batchSize: 100, confirmTimeoutMs: 30000);

    // 4) 应用标识(可选)
    c.WithApplication("YourAppName");

    // 5) 处理器并发控制(全局设置,非顺序执行时生效)
    // HandlerMaxDegreeOfParallelism: 单个事件处理器最大并发度,防止CPU过载(默认: 4)
    c.WithHandlerConcurrency(handlerMaxDegreeOfParallelism: 4);

    // ===== 事件配置 =====
    // 5.1 Routing(直连)示例
    c.AddEvent<TestEvent>(EModel.Routing, exchangeName: "test.exchange", routingKey: "test.key", queueName: "test.queue")
     .WithEventQos(prefetchCount: 20)  // 事件级QoS设置
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

    // 批量发布消息(提高吞吐量)
    var events = Enumerable.Range(1, 100).Select(i => new TestEvent { Message = $"batch-{i}" });
    await _bus.PublishBatch(events);

    // 批量发布带自定义路由键
    await _bus.PublishBatch(events, routingKey: "batch.topic");

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
            .WithResolver(CompositeResolver.Create(NativeDateTimeResolver.Instance, ContractlessStandardResolver.Instance))
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

- **延迟队列**: 必须安装 rabbitmq-delayed-message-exchange 插件; 框架会自动为延迟交换机声明 x-delayed-type=direct
- **优先级队列**: 若使用优先级,请为队列设置参数 x-max-priority
- **默认交换机**: EModel.None 表示不显式声明交换机,使用默认交换机; 此时 routingKey 应为队列名
- **路由键覆盖**: Publish 的 routingKey 参数用于覆盖事件配置中的路由键,便于 Topic 下多路由生产者发信
- **顺序执行**: 可通过 ConfigureEvent(ec => ec.SequentialHandlerExecution = true) 开启同一事件多个处理器的顺序执行
- **并发控制**: HandlerMaxDegreeOfParallelism 用于控制单个事件处理器的最大并发度,防止 CPU 过载(默认值: 4)
- **发布确认**: PublisherConfirms 启用后会影响发布性能,但能确保消息可靠投递
- **批量发布**: 使用 PublishBatch 方法减少网络往返次数
- **调整 BatchSize**: 根据消息大小和网络延迟调整批量大小 (默认: 100, 建议: 50-500)
- **禁用 PublisherConfirms**: 生产环境如不需要绝对可靠性可禁用以提升性能
- **QoS 设置**: PrefetchCount 限制未确认消息的数量,ConsumerDispatchConcurrency 控制消费者调度并发数
- **错误处理**: 框架内置重试机制和超时策略,失败的消息会根据配置进行重试
- **交换机验证**: 默认在启动阶段验证交换机存在且类型匹配(ValidateExchangesOnStartup=true); 若外部统一声明交换机,可设置 SkipExchangeDeclare=true 跳过声明;
  若类型不匹配,会 fail fast 并给出清晰日志(inequivalent arg 'type').
- **发布背压**: 当启用发布确认时,若未确认数量达到 MaxOutstandingConfirms 将主动等待(微短延迟)以保护内存与确认队列。
  可按吞吐与内存权衡合理调整该阈值(默认 1000)。
- **重试与死信**: 发布确认 Nack 或确认超时的消息将进入后台重试,采用指数退避(+抖动)。超过最大重试次数后将写入死信存储(默认内存)。

#### 高级配置示例

##### 顺序执行处理器

```csharp
// 同一事件多个处理器按顺序执行(默认并行)
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .ConfigureEvent(ec => ec.SequentialHandlerExecution = true) // 顺序执行
 .And();
```

##### 高性能配置(高吞吐量场景)

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 高并发消费者设置
    c.WithConsumerSettings(dispatchConcurrency: 50, prefetchCount: 200);

    // 禁用发布确认以提升性能(生产环境可根据需要启用)
    c.WithResilience(retryCount: 3, publisherConfirms: false);

    // 提高处理器并发度
    c.WithHandlerConcurrency(handlerMaxDegreeOfParallelism: 8);

    // 事件配置
    c.AddEvent<HighVolumeEvent>(EModel.Routing, "highvol.exchange", "highvol.key", "highvol.queue")
     .WithEventQos(prefetchCount: 100) // 事件级QoS
     .And();
});
```

##### 高可靠性配置(金融/关键业务场景)

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 保守的消费者设置
    c.WithConsumerSettings(dispatchConcurrency: 5, prefetchCount: 20);

    // 启用发布确认确保消息不丢失
    c.WithResilience(retryCount: 10, publisherConfirms: true);

    // 降低并发度保证稳定性
    c.WithHandlerConcurrency(handlerMaxDegreeOfParallelism: 2);

    // 事件配置
    c.AddEvent<CriticalEvent>(EModel.Routing, "critical.exchange", "critical.key", "critical.queue")
     .WithEventQos(prefetchCount: 10)
     .ConfigureEvent(ec => ec.SequentialHandlerExecution = true) // 顺序处理保证一致性
     .And();
});
```

##### 集群连接配置

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 集群连接(多个节点)
    c.WithConnection(f =>
    {
        f.HostName = "rabbitmq-cluster";
        f.UserName = "user";
        f.Password = "password";
        f.Port = 5672;
        f.VirtualHost = "/";
        // 如使用 AmqpTcpEndpoints, 可在 builder 中提供多个节点
    });

    // 其他配置...
});
```

#### 健康检查与可观测性

- 健康检查

  - 已自动注册 `RabbitBusHealthCheck`。若你启用了 ASP.NET Core 健康检查端点,只需在管道中映射:

  ```csharp
  // Program.cs
  builder.Services.AddHealthChecks(); // 若外部未调用,库内部也会注册
  var app = builder.Build();
  app.MapHealthChecks("/health");
  ```

- 指标(基于 System.Diagnostics.Metrics)

  - Meter 名称: `EasilyNET.RabbitBus`
  - 关键指标:

    - 发布: `rabbitmq_published_normal_total`, `rabbitmq_published_delayed_total`, `rabbitmq_published_batch_events_total`
    - 确认: `rabbitmq_publisher_ack_total`, `rabbitmq_publisher_nack_total`, `rabbitmq_publisher_confirm_timeout_total`, `rabbitmq_outstanding_confirms`
    - 重试: `rabbitmq_retry_enqueued_total`, `rabbitmq_retry_attempt_total`, `rabbitmq_retry_discarded_total`, `rabbitmq_retry_rescheduled_total`
    - 连接: `rabbitmq_connection_reconnect_total`
    - 死信: `rabbitmq_deadletter_total`

  - 快速观察(开发):

  ```bash
  dotnet-counters monitor --process <your-app-pid> --counters EasilyNET.RabbitBus
  ```

  - OpenTelemetry: 按常规方式接入 OTLP/Prometheus 导出器即可收集上述指标。

| `WithConnection` | - | - | RabbitMQ 连接配置(主机、端口、认证等) |
| `WithConsumerSettings` | `dispatchConcurrency` | 10 | 消费者调度并发数,控制同时处理的消息数 |
| | `prefetchCount` | 100 | QoS 预取计数,限制未确认消息的数量 |
| `WithResilience` | `retryCount` | 5 | 消息处理失败时的重试次数 |
| | `publisherConfirms` | true | 是否启用发布确认模式 |

#### 发布限流/背压

- 当启用发布确认(PublisherConfirms=true)时,框架会以 `MaxOutstandingConfirms` 为阈值控制未确认发布数量。
- 若达到阈值,发布线程将进行短暂等待,直到确认数下降,以防止内存暴涨或确认集合过大。
- 建议根据发布速率与确认延迟进行压测,选择合适的阈值(默认 1000, 常见范围 500~5000)。

#### 交换机声明与验证

- `SkipExchangeDeclare=true` 时,框架不会主动声明交换机,仅在需要时进行被动验证或直接发布(取决于场景)。
- `ValidateExchangesOnStartup=true` 时,启动阶段会被动(passive)验证交换机是否存在且类型匹配; 若类型不一致,会明确报错并终止启动(避免运行期频繁连接被关闭)。
- 若你在外部工具或基础设施层统一声明交换机,建议开启 `SkipExchangeDeclare` 以减少不必要的声明开销。

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 集群连接(多个节点)
    c.WithConnection(f =>
    {
        f.HostName = "rabbitmq-cluster";
        f.UserName = "user";
        f.Password = "password";
        f.Port = 5672;
        // 集群节点
        f.VirtualHost = "/";
    });

    // 其他配置...
});
```

#### 性能调优指南

##### 吞吐量优化

- **增加 ConsumerDispatchConcurrency**: 提高消费者调度并发数 (默认: 10, 建议: 10-50)
- **调整 PrefetchCount**: 根据消息处理速度调整预取数量 (默认: 100, 建议: 50-200)
- **HandlerMaxDegreeOfParallelism**: 提高处理器并发度 (默认: 4, 建议: 4-16)
- **禁用 PublisherConfirms**: 生产环境如不需要绝对可靠性可禁用以提升性能

##### CPU 使用率控制

- **降低 HandlerMaxDegreeOfParallelism**: 防止 CPU 过载 (建议: 2-8)
- **启用 SequentialHandlerExecution**: 顺序执行减少并发开销
- **监控系统负载**: 根据实际 CPU 使用率调整并发参数

##### 内存优化

- **合理设置 PrefetchCount**: 避免内存积压
- **监控队列长度**: 及时处理积压消息
- **调整重试次数**: 减少无效重试的内存占用

#### 故障排除

##### 常见问题

1. **连接失败**

   - 检查连接字符串格式
   - 确认 RabbitMQ 服务运行状态
   - 验证用户名密码和虚拟主机权限

2. **消息丢失**

   - 启用 PublisherConfirms
   - 检查消费者是否正确处理消息
   - 确认队列和交换机正确声明

3. **性能问题**

   - 检查 ConsumerDispatchConcurrency 设置
   - 监控 HandlerMaxDegreeOfParallelism 使用率
   - 分析消息处理时间瓶颈

4. **内存泄漏**
   - 确保消息正确确认(ack)
   - 检查处理器是否及时释放资源
   - 监控连接和通道数量

#### 版本兼容性

- **延迟队列**: 需要 rabbitmq-delayed-message-exchange 插件

#### 配置参数参考表

| 配置方法                 | 参数                            | 默认值           | 说明                                  |
| ------------------------ | ------------------------------- | ---------------- | ------------------------------------- |
| `WithConnection`         | -                               | -                | RabbitMQ 连接配置(主机、端口、认证等) |
| `WithConsumerSettings`   | `dispatchConcurrency`           | 10               | 消费者调度并发数,控制同时处理的消息数 |
|                          | `prefetchCount`                 | 100              | QoS 预取计数,限制未确认消息的数量     |
| `WithResilience`         | `retryCount`                    | 5                | 消息处理失败时的重试次数              |
|                          | `publisherConfirms`             | true             | 是否启用发布确认模式                  |
|                          | `maxOutstandingConfirms`        | 1000             | 最大未确认发布数量                    |
|                          | `batchSize`                     | 100              | 批量发布大小                          |
|                          | `confirmTimeoutMs`              | 30000            | 发布确认超时时间(毫秒)                |
| `WithConnection`/全局    | `ReconnectIntervalSeconds`      | 15               | 基础重连间隔(指数退避 + 抖动)         |
| `WithApplication`        | `appName`                       | -                | 应用标识,用于日志和监控               |
| `WithHandlerConcurrency` | `handlerMaxDegreeOfParallelism` | 4                | 单个事件处理器最大并发度              |
| `WithEventQos`           | `prefetchCount`                 | -                | 事件级 QoS 设置(覆盖全局设置)         |
| `WithEventHeaders`       | `headers`                       | -                | 消息头参数                            |
| `WithEventQueueArgs`     | `args`                          | -                | 队列声明参数(x-max-priority 等)       |
| `WithEventExchangeArgs`  | `args`                          | -                | 交换机声明参数                        |
| `ConfigureEvent`         | `SequentialHandlerExecution`    | false            | 是否顺序执行同一事件的多个处理器      |
| `IgnoreHandler`          | -                               | -                | 忽略指定的处理器                      |
| `WithSerializer`         | -                               | System.Text.Json | 自定义消息序列化器                    |
| 全局                     | `SkipExchangeDeclare`           | false            | 跳过交换机声明(外部已声明时可启用)    |
| 全局                     | `ValidateExchangesOnStartup`    | true             | 启动阶段验证交换机类型与存在性        |

#### 最佳实践

1. **生产环境建议**

   - 启用 PublisherConfirms 确保消息可靠性
   - 根据业务场景调整并发参数
   - 监控系统资源使用率
   - 设置合理的重试次数和超时时间
   - 使用批量发布提高高吞吐量场景性能

2. **开发环境建议**

   - 使用较低的并发设置便于调试
   - 启用详细日志记录
   - 使用默认序列化器简化开发
   - 测试批量发布功能

3. **性能监控**

   - 监控消息处理延迟
   - 跟踪消费者连接状态
   - 观察内存和 CPU 使用率
   - 定期检查队列积压情况
   - 监控发布确认的成功率

4. **错误处理**
   - 实现全局异常处理器
   - 配置死信存储/队列处理失败消息
   - 设置监控告警机制
   - 记录详细的错误日志
