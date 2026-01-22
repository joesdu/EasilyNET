#### EasilyNET.RabbitBus.AspNetCore

支持延时队列（需要启用 rabbitmq-delayed-message-exchange 插件）

- 支持同一个消息被多个 Handler 消费（可配置并发或顺序执行）
- 支持忽略指定 Handler
- 支持事件级 QoS、Headers、交换机/队列参数、优先级队列
- 支持发布确认（Publisher Confirms）与发布背压
- 支持批量发布提升吞吐量
- 现代流式配置：无需在事件/处理器上标注特性
- 内建发布失败重试（Nack/Confirm 超时）后台调度器：指数退避 + 抖动
- 死信存储：超过最大重试后写入死信存储（内置内存实现，支持自定义）
- 健康检查与可观测性：连接/发布/重试等指标 + 健康检查

##### 如何使用

- 使用 NuGet 包管理工具添加依赖 EasilyNET.RabbitBus.AspNetCore

###### Step 1. 在 Program.cs 中注册消息总线（现代流式配置）

```csharp
// Program.cs / Startup
builder.Services.AddRabbitBus(c =>
{
    // 1) 连接配置（支持连接串/单点/集群）
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!));

    // 2) 消费者默认设置
    // dispatchConcurrency: ConsumerDispatchConcurrency（默认 10）
    // prefetchCount/prefetchSize/global: QoS（默认 100/0/false）
    // consumerChannelLimit: 消费者通道上限（0 不限制）
    c.WithConsumerSettings(dispatchConcurrency: 10, prefetchCount: 100, prefetchSize: 0, global: false, consumerChannelLimit: 0);

    // 3) 弹性与发布确认
    // retryCount/retryIntervalSeconds: 重试次数与后台重试间隔
    // publisherConfirms: 发布确认（默认 true）
    // maxOutstandingConfirms: 最大未确认发布数（默认 1000）
    // batchSize: 批量发布大小（默认 100）
    // confirmTimeoutMs: 发布确认超时（默认 30000ms）
    c.WithResilience(retryCount: 5, retryIntervalSeconds: 1, publisherConfirms: true, maxOutstandingConfirms: 1000, batchSize: 100, confirmTimeoutMs: 30000);

    // 4) 交换机声明/验证
    // 注意：若你调用了 WithExchangeSettings() 且未传参，则 validateExchangesOnStartup 将变为 false
    c.WithExchangeSettings(skipExchangeDeclare: false, validateExchangesOnStartup: true);

    // 5) 重试队列容量（可选）
    c.WithRetryQueueSizing(maxSize: null, memoryRatio: 0.02, avgEntryBytes: 2048);

    // 6) 应用标识（可选）
    c.WithApplication("YourAppName");

    // ===== 事件配置 =====
    c.AddEvent<TestEvent>(EModel.Routing, exchangeName: "test.exchange", routingKey: "test.key", queueName: "test.queue")
     .WithEventQos(prefetchCount: 20)
     .WithEventHeaders(new() { ["x-version"] = "v1" })
     .WithEventQueueArgs(new() { ["x-max-priority"] = 9 })
     .WithEventExchangeArgs(new() { ["alternate-exchange"] = "alt.exchange" })
     .WithHandler<TestEventHandler>()
     .WithHandler<TestEventHandlerSecond>()
     .And();

    // 延迟消息示例（需要 rabbitmq-delayed-message-exchange）
    c.AddEvent<DelayedMessageEvent>(EModel.Delayed, exchangeName: "delayed.exchange", routingKey: "delayed.key", queueName: "delayed.queue")
     .WithHandler<DelayedMessageHandler>();

    // 发布/订阅（Fanout）
    c.AddEvent<FanoutEvent>(EModel.PublishSubscribe, "fanout.exchange", queueName: "fanout.queue")
     .WithHandler<FanoutEventHandler>();

    // 忽略某个处理器
    c.IgnoreHandler<TestEvent, TestEventHandlerSecond>();

    // 自定义序列化器（可选）
    // c.WithSerializer<MsgPackSerializer>();
});
```

###### Step 2. 定义事件与处理器（无需特性）

```csharp
using EasilyNET.RabbitBus.Core;
using EasilyNET.RabbitBus.Core.Abstraction;

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
    // 普通消息（按事件配置路由）
    await _bus.Publish(new TestEvent { Message = "normal" });

    // 显式指定 routingKey（Topic/多路由场景）
    await _bus.Publish(new TestEvent { Message = "topic" }, routingKey: "topic.queue.1");

    // 使用优先级（需队列设置 x-max-priority）
    await _bus.Publish(new TestEvent { Message = "priority" }, priority: 5);

    // 批量发布
    var events = Enumerable.Range(1, 100).Select(i => new TestEvent { Message = $"batch-{i}" });
    await _bus.PublishBatch(events);

    // 批量发布 + 自定义路由
    await _bus.PublishBatch(events, routingKey: "batch.topic");

    // 延迟消息（仅 EModel.Delayed）
    await _bus.Publish(new DelayedMessageEvent { Message = "delay-5s" }, ttl: 5000);

    // 延迟消息（TimeSpan 重载）
    await _bus.Publish(new DelayedMessageEvent { Message = "delay-2s" }, TimeSpan.FromSeconds(2));
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

- **事件必须注册处理器**：仅通过 `WithHandler<THandler>()` 明确注册的处理器才会创建消费者并注入 DI。
- **处理器执行顺序**：同一事件的多个处理器支持两种执行模式：
  - **并发执行**（默认）：处理器并行执行，提高吞吐量
  - **顺序执行**：通过 `SequentialHandlerExecution = true` 配置，确保处理器按注册顺序依次执行
- **并发方式**：提高 `HandlerThreadCount`（每事件消费者数量）以及 `ConsumerDispatchConcurrency` 以提升并发；`ConsumerChannelLimit` 可限制通道数量。
- **延迟队列**：必须安装 rabbitmq-delayed-message-exchange 插件；框架会自动为延迟交换机声明 `x-delayed-type=direct`。
- **优先级队列**：使用优先级需设置队列参数 `x-max-priority`。
- **默认交换机**：`EModel.None` 表示不显式声明交换机，使用默认交换机；此时 routingKey 默认为队列名。
- **路由键覆盖**：`Publish` 的 `routingKey` 参数可覆盖事件配置中的路由键，便于 Topic 多路由。
- **发布确认**：启用 PublisherConfirms 会影响发布性能，但能确保可靠投递；禁用可提升吞吐。
- **批量发布**：使用 `PublishBatch` 减少网络往返；根据消息大小调整 `BatchSize`（默认 100，建议 50-500）。
- **交换机验证**：默认启动阶段验证交换机存在且类型匹配（`ValidateExchangesOnStartup=true`）；若外部统一声明交换机，可设 `SkipExchangeDeclare=true` 跳过声明。
- **发布背压**：启用发布确认时，未确认数量达到 `MaxOutstandingConfirms` 会等待以保护内存。
- **重试与死信**：确认 Nack 或确认超时会进入后台重试队列（指数退避 + 抖动）；超过 `RetryCount` 后写入死信存储。可通过实现 `IDeadLetterStore` 接口自定义死信存储（如数据库、Redis 等）。

#### 处理器执行模式

支持两种处理器执行模式，通过事件配置中的 `SequentialHandlerExecution` 属性控制：

```csharp
// 顺序执行：处理器按注册顺序依次执行（适用于有执行顺序依赖的场景）
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .ConfigureEvent(cfg => cfg.SequentialHandlerExecution = true)
 .WithHandler<OrderValidationHandler>()   // 第一步：验证
 .WithHandler<OrderProcessingHandler>()   // 第二步：处理
 .WithHandler<OrderNotificationHandler>() // 第三步：通知
 .And();

// 并发执行（默认）：处理器并行执行，提高吞吐量
c.AddEvent<LogEvent>(EModel.Routing, "log.exchange", "log.key", "log.queue")
 .WithHandler<ConsoleLogHandler>()
 .WithHandler<FileLogHandler>()
 .WithHandler<DatabaseLogHandler>()
 .And();
```

| 模式 | 配置 | 特点 | 适用场景 |
|------|------|------|----------|
| 并发执行 | `SequentialHandlerExecution = false`（默认） | 处理器并行执行，高吞吐 | 处理器之间无依赖关系 |
| 顺序执行 | `SequentialHandlerExecution = true` | 按注册顺序依次执行 | 处理器有执行顺序依赖 |

#### 自定义死信存储

`IDeadLetterStore` 是公开接口，可实现自定义死信存储（如 Redis、数据库等）：

```csharp
using EasilyNET.RabbitBus.AspNetCore.Abstractions;
using EasilyNET.RabbitBus.Core.Abstraction;

/// <summary>
/// Redis 死信存储示例
/// </summary>
public class RedisDeadLetterStore : IDeadLetterStore
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDeadLetterStore(IConnectionMultiplexer redis) => _redis = redis;

    public async ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var key = $"deadletter:{message.EventType}:{message.EventId}";
        var value = JsonSerializer.Serialize(new
        {
            message.EventType,
            message.EventId,
            message.CreatedUtc,
            message.RetryCount,
            OriginalEvent = message.OriginalEvent
        });
        await db.StringSetAsync(key, value);
    }

    public async IAsyncEnumerable<IDeadLetterMessage> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "deadletter:*"))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                // 反序列化并返回 IDeadLetterMessage 实例
                yield return DeserializeMessage(value!);
            }
        }
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: "deadletter:*"))
        {
            await db.KeyDeleteAsync(key);
        }
    }

    private static IDeadLetterMessage DeserializeMessage(string json)
    {
        // 实现反序列化逻辑...
        throw new NotImplementedException();
    }
}

// 注册自定义死信存储（替换内置的 InMemoryDeadLetterStore）
builder.Services.AddSingleton<IDeadLetterStore, RedisDeadLetterStore>();
```

**IDeadLetterMessage 接口成员**：

| 属性 | 类型 | 说明 |
|------|------|------|
| `EventType` | `string` | 事件类型名称 |
| `EventId` | `string` | 事件唯一标识 |
| `CreatedUtc` | `DateTime` | 消息创建时间（UTC） |
| `RetryCount` | `int` | 进入死信前的重试次数 |
| `OriginalEvent` | `IEvent` | 原始事件实例 |

#### 高级配置示例

##### 处理器并发（多消费者）

```csharp
// 通过增加 HandlerThreadCount 创建多个消费者通道并行消费
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .WithHandlerThreadCount(4)
 .WithHandler<OrderEventHandler>()
 .And();
```

##### 高性能配置（高吞吐场景）

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 高并发消费者设置
    c.WithConsumerSettings(dispatchConcurrency: 50, prefetchCount: 200);

    // 禁用发布确认以提升性能(生产环境可根据需要启用)
    c.WithResilience(retryCount: 3, publisherConfirms: false);

    // 增加消费者并发
    // 事件级通过 WithHandlerThreadCount 调整（见下方示例）

    // 事件配置
    c.AddEvent<HighVolumeEvent>(EModel.Routing, "highvol.exchange", "highvol.key", "highvol.queue")
     .WithEventQos(prefetchCount: 100)
     .WithHandlerThreadCount(8)
     .WithHandler<HighVolumeEventHandler>()
     .And();
});
```

##### 高可靠性配置（金融/关键业务场景）

```csharp
builder.Services.AddRabbitBus(c =>
{
    // 保守的消费者设置
    c.WithConsumerSettings(dispatchConcurrency: 5, prefetchCount: 20);

    // 启用发布确认确保消息不丢失
    c.WithResilience(retryCount: 10, publisherConfirms: true);

    // 事件配置
    c.AddEvent<CriticalEvent>(EModel.Routing, "critical.exchange", "critical.key", "critical.queue")
     .WithEventQos(prefetchCount: 10)
      .WithHandlerThreadCount(1)
      .WithHandler<CriticalEventHandler>()
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

- 指标（基于 System.Diagnostics.Metrics）
  - Meter 名称: `EasilyNET.RabbitBus`
    - 关键指标（Meter 实际名称，已采用点分式命名规范）：
      - 发布: `rabbitmq.publish.normal.total`, `rabbitmq.publish.delayed.total`, `rabbitmq.publish.retried.total`, `rabbitmq.publish.discarded.total`
      - 确认: `rabbitmq.publish.confirm.ack.total`, `rabbitmq.publish.confirm.nack.total`, `rabbitmq.publish.outstanding.confirms`
      - 重试: `rabbitmq.retry.enqueued.total`
      - 连接: `rabbitmq.connection.reconnects.total`, `rabbitmq.connection.active`, `rabbitmq.channel.active`, `rabbitmq.connection.state`
      - 死信: `rabbitmq.deadletter.total`
      > 说明（EN）：These are the latest dot-separated metric names. Older versions used underscore-based names (for example: `rabbitmq_published_normal_total`). If you previously collected metrics via the old names, please update your dashboards/alerts accordingly.
      > 说明（中文）：以上为最新的点分式指标命名规范，已从旧的下划线风格（例如：`rabbitmq_published_normal_total`）迁移而来。如你已基于旧名称配置监控/告警，请同步更新对应配置。

  - 快速观察(开发):

  ```bash
  dotnet-counters monitor --process <your-app-pid> --counters EasilyNET.RabbitBus
  ```

- OpenTelemetry: 按常规方式接入 OTLP/Prometheus 导出器即可收集上述指标。

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
- **WithHandlerThreadCount**: 通过增加每事件消费者数量提升吞吐
- **禁用 PublisherConfirms**: 生产环境如不需要绝对可靠性可禁用以提升性能

##### CPU 使用率控制

- **降低 HandlerThreadCount**: 防止 CPU 过载
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
   - 调整 HandlerThreadCount 与 PrefetchCount
   - 分析消息处理时间瓶颈

4. **内存泄漏**
   - 确保消息正确确认(ack)
   - 检查处理器是否及时释放资源
   - 监控连接和通道数量

#### 版本兼容性

- **延迟队列**: 需要 rabbitmq-delayed-message-exchange 插件

#### 配置参数参考表

| 配置方法                 | 参数                             | 默认值                | 说明                                  |
| ------------------------ | -------------------------------- | --------------------- | ------------------------------------- |
| `WithConnection`         | -                                | -                     | RabbitMQ 连接配置(主机、端口、认证等) |
| `WithConsumerSettings`   | `dispatchConcurrency`            | 10                    | 消费者调度并发数,控制同时处理的消息数 |
|                          | `prefetchCount`                  | 100                   | QoS 预取计数,限制未确认消息数量       |
|                          | `prefetchSize`                   | 0                     | QoS 预取大小                          |
|                          | `global`                         | false                 | QoS 是否全局                          |
|                          | `consumerChannelLimit`           | 0                     | 消费者通道上限(0 表示不限制)          |
| `WithResilience`         | `retryCount`                     | 5                     | 发布失败/确认失败的重试次数           |
|                          | `retryIntervalSeconds`           | 1                     | 后台重试检查间隔(秒)                  |
|                          | `publisherConfirms`              | true                  | 是否启用发布确认模式                  |
|                          | `maxOutstandingConfirms`         | 1000                  | 最大未确认发布数量                    |
|                          | `batchSize`                      | 100                   | 批量发布大小                          |
|                          | `confirmTimeoutMs`               | 30000                 | 发布确认超时时间(毫秒)                |
| `WithExchangeSettings`   | `skipExchangeDeclare`            | false                 | 跳过交换机声明(外部已声明时可启用)    |
|                          | `validateExchangesOnStartup`     | false                 | 启动验证交换机类型(调用该方法时默认)  |
| `WithRetryQueueSizing`   | `maxSize`                        | -                     | 固定最大重试队列长度(>0 生效)         |
|                          | `memoryRatio`                    | 0.02                  | 估算队列内存占比(0-0.25)              |
|                          | `avgEntryBytes`                  | 2048                  | 单条重试项估算字节数                  |
| `WithApplication`        | `appName`                        | `MachineName, EasilyNET` | 应用标识,用于日志与指标标签           |
| `WithEventQos`           | `prefetchCount`                  | -                     | 事件级 QoS 设置(覆盖全局设置)         |
|                          | `prefetchSize`                   | -                     | 事件级 QoS 预取大小                   |
|                          | `global`                         | -                     | 事件级 QoS 是否全局                   |
| `WithEventHeaders`       | `headers`                        | -                     | 消息头参数                            |
| `WithEventQueueArgs`     | `args`                           | -                     | 队列声明参数(x-max-priority 等)       |
| `WithEventExchangeArgs`  | `args`                           | -                     | 交换机声明参数                        |
| `WithHandler`            | `THandler`                       | -                     | 注册事件处理器(必须)                  |
| `WithHandlerThreadCount` | `threadCount`                    | 1                     | 该事件消费者数量(并行度)              |
| `ConfigureEvent`         | `SequentialHandlerExecution`     | false                 | 是否按顺序执行处理器                  |
|                          | `Exchange/Queue/Qos/Headers/...` | -                     | 事件高级配置入口                      |
| `IgnoreHandler`          | -                                | -                     | 忽略指定的处理器                      |
| `WithSerializer`         | -                                | System.Text.Json      | 自定义消息序列化器                    |
| 全局                     | `SkipExchangeDeclare`            | false                 | 跳过交换机声明(外部已声明时可启用)    |
| 全局                     | `ValidateExchangesOnStartup`     | false                 | 启动阶段验证交换机类型与存在性        |

#### 内部架构说明

> 以下为高级内容，普通使用者无需关注。

##### Channel<T> 重试队列

框架内部使用 `System.Threading.Channels.Channel<T>` 实现高性能的重试消息队列：

- **无锁设计**：相比 `ConcurrentQueue<T>`，Channel 提供更好的异步消费体验
- **背压支持**：结合信号量实现发布限流，防止内存溢出
- **零分配热路径**：使用 `struct RetryMessage` 减少 GC 压力

##### Polly ResiliencePipeline

框架使用 Polly v8+ 的 `ResiliencePipeline` 实现弹性策略：

- **PublishPipeline**：发布操作的重试、超时策略
- **ConnectionPipeline**：连接建立的重试策略
- **HandlerPipeline**：消息处理器的重试、超时策略

```csharp
// 内部注册示例（仅供参考）
services.AddResiliencePipeline(Constant.HandlerPipelineName, (builder, context) =>
{
    builder.AddRetry(new()
    {
        ShouldHandle = new PredicateBuilder()
                       .Handle<BrokerUnreachableException>()
                       .Handle<SocketException>()
                       .Handle<TimeoutException>(),
        MaxRetryAttempts = 2,
        Delay = TimeSpan.FromSeconds(1),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true
    });
    builder.AddTimeout(TimeSpan.FromSeconds(30));
});
```

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
