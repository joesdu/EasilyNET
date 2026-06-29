#### EasilyNET.RabbitBus

> 📦 **包重命名**：本包由 `EasilyNET.RabbitBus.AspNetCore` 重命名而来，已不再依赖 ASP.NET Core 共享框架，可用于任意 .NET 宿主（Worker / 控制台 / 通用 Host）。迁移要点：包名 `EasilyNET.RabbitBus.AspNetCore` → `EasilyNET.RabbitBus`；命名空间 `EasilyNET.RabbitBus.AspNetCore.*` → `EasilyNET.RabbitBus.*`（注册扩展仍位于 `Microsoft.Extensions.DependencyInjection`，通常无需改 using）；公共 API 不变。

- 支持同一个消息被多个 Handler 消费（可配置并发或顺序执行）
- 支持忽略指定 Handler
- 支持事件级 QoS、Headers 交换机、交换机/队列参数、优先级队列
- 支持发布确认（Publisher Confirms）与发布背压
- 支持批量发布提升吞吐量
- 现代流式配置：无需在事件/处理器上标注特性
- 内建发布失败重试（Nack/Confirm 超时）后台调度器：指数退避 + 抖动
- 死信存储：超过最大重试后写入死信存储（内置内存实现，支持自定义）
- 健康检查与可观测性：连接/发布/重试等指标 + 健康检查
- 消费者中间件管道：支持事务、幂等性检查、日志等横切关注点
- 消费者回退处理器：重试耗尽后自定义消息处置（Ack/Nack/Requeue/DeadLetter）
- 处理器显式排序：通过 `order` 参数控制顺序执行时的处理器顺序
- 自定义弹性管道：每个事件可配置独立的重试/超时策略
- OpenTelemetry 分布式追踪：发布/消费链路自动传播 trace context

#### 关于延迟消息功能的移除说明

本库已移除对 RabbitMQ 延迟消息交换机（`rabbitmq-delayed-message-exchange`）插件的支持。

**原因**：RabbitMQ 官方团队已于 2026 年 1 月 29 日宣布停止维护该插件，主要原因如下：

1. **严重的设计限制**：该插件基于 Mnesia 存储，存在单节点限制，无法在集群中复制延迟消息，节点故障会导致消息丢失
2. **不适合大规模使用**：当前设计不适合处理大量延迟消息（如数十万或数百万条）
3. **Mnesia 将被移除**：RabbitMQ 从 4.3 或 4.4 版本开始将移除 Mnesia，该插件将无法继续工作
4. **重新设计成本过高**：分布式设计需要从自定义交换机类型切换到自定义队列类型，需要数人年的研发投入

**替代方案**：

- 使用 RabbitMQ 的 [死信交换机（DLX）](https://www.rabbitmq.com/docs/dlx) + [TTL](https://www.rabbitmq.com/docs/ttl)
  组合实现基本的延迟和重试功能
- 使用外部调度器和适合长期存储的数据库

> 参考：[rabbitmq/rabbitmq-delayed-message-exchange](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)

##### 如何使用

- 使用 NuGet 包管理工具添加依赖 EasilyNET.RabbitBus

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
     .WithMiddleware<TestEventMiddleware>()          // 可选：中间件（事务/幂等性）
     .WithFallbackHandler<TestEventFallbackHandler>() // 可选：回退处理器
     .WithHandler<TestEventHandler>()
     .WithHandler<TestEventHandlerSecond>()
     .And();

    // 发布/订阅（Fanout）
    c.AddEvent<FanoutEvent>(EModel.PublishSubscribe, "fanout.exchange", queueName: "fanout.queue")
     .WithHandler<FanoutEventHandler>();

    // Headers 交换机（基于消息头属性匹配路由）
    c.AddEvent<HeadersEvent>(EModel.Headers, "headers.exchange", queueName: "headers.queue")
     .WithBindingArguments(new() { ["x-match"] = "all", ["format"] = "pdf", ["type"] = "report" })
     .WithEventHeaders(new() { ["format"] = "pdf", ["type"] = "report" })
     .WithHandler<HeadersEventHandler>()
     .And();

    // 顺序执行 + 显式排序
    c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
     .ConfigureEvent(cfg => cfg.SequentialHandlerExecution = true)
     .WithMiddleware<OrderEventMiddleware>()
     .WithFallbackHandler<OrderFallbackHandler>()
     .WithHandler<OrderValidationHandler>(order: 0)
     .WithHandler<OrderProcessingHandler>(order: 10)
     .WithHandler<OrderNotificationHandler>(order: 20)
     .And();

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

#### Headers 交换机

Headers 交换机根据消息头属性（而非 routing key）进行路由，适用于多维度内容过滤场景。

- **绑定参数**（`WithBindingArguments`）：定义队列绑定时的匹配规则，包含 `x-match`（`all` 或 `any`）和匹配键值对
- **消息头**（`WithEventHeaders`）：定义发布时携带的头部键值对，用于与绑定参数进行匹配
- `x-match=all`：消息头必须包含绑定中所有键值对才匹配
- `x-match=any`：消息头只要包含绑定中任意一个键值对即匹配

```csharp
// 定义事件
public class PdfReportEvent : Event
{
    public string Content { get; set; } = default!;
}

// 注册事件（x-match=all：必须同时匹配 format=pdf 和 type=report）
c.AddEvent<PdfReportEvent>(EModel.Headers, "report.headers.exchange", queueName: "pdf-reports")
 .WithBindingArguments(new() { ["x-match"] = "all", ["format"] = "pdf", ["type"] = "report" })
 .WithEventHeaders(new() { ["format"] = "pdf", ["type"] = "report" })
 .WithHandler<PdfReportHandler>()
 .And();

// 注册事件（x-match=any：匹配 region=asia 或 priority=high 任意一个即可）
c.AddEvent<UrgentEvent>(EModel.Headers, "urgent.headers.exchange", queueName: "urgent-queue")
 .WithBindingArguments(new() { ["x-match"] = "any", ["region"] = "asia", ["priority"] = "high" })
 .WithEventHeaders(new() { ["region"] = "asia" })
 .WithHandler<UrgentEventHandler>()
 .And();
```

> **注意**：Headers 交换机完全忽略 routing key，路由仅依赖消息头与绑定参数的匹配。性能上比 direct/topic 交换机稍差（需逐个匹配
> header 键值对），适合多维度过滤场景。

#### 注意事项

- **事件必须注册处理器**：仅通过 `WithHandler<THandler>()` 明确注册的处理器才会创建消费者并注入 DI。
- **处理器生命周期**：处理器注册为 Scoped 生命周期，每条消息创建独立的 DI 作用域，可安全注入 DbContext 等 Scoped 服务。
  > **⚠️ 破坏性变更（Breaking Change）**：处理器（Handler）、中间件（Middleware）和回退处理器（FallbackHandler）的 DI 生命周期已从
  **Singleton 变更为 Scoped**。如果你的处理器依赖 Singleton 语义（如内部维护可变状态），请改用注入的 Singleton
  服务来管理共享状态。此变更是为了正确支持每条消息独立的 DI 作用域，使处理器可以安全注入 `DbContext` 等 Scoped
  服务。此外，中间件和回退处理器在 DI 解析失败时将抛出 `InvalidOperationException` 而非静默降级，以确保显式配置的组件不会被意外跳过。
- **处理器执行顺序**：同一事件的多个处理器支持两种执行模式：
    - **并发执行**（默认）：处理器并行执行，提高吞吐量
    - **顺序执行**：通过 `SequentialHandlerExecution = true` 配置，确保处理器按 `order` 参数排序后依次执行
- **中间件管道**：通过 `WithMiddleware<T>()` 注册中间件，包裹整个处理器链路。适用于事务、幂等性检查、审计日志等横切关注点。
- **回退处理器**：通过 `WithFallbackHandler<T>()` 注册回退处理器，在所有重试耗尽后决定消息的处置方式（Ack/Nack/Requeue/DeadLetter）。
- **分布式追踪**：框架自动为发布/消费创建 OpenTelemetry span，通过消息头传播 trace context。只需
  `AddSource("EasilyNET.RabbitBus")` 即可接入。
- **并发方式**：提高 `HandlerThreadCount`（每事件消费者数量）以及 `ConsumerDispatchConcurrency` 以提升并发；
  `ConsumerChannelLimit` 可限制通道数量。
- **优先级队列**：使用优先级需设置队列参数 `x-max-priority`。
- **默认交换机**：`EModel.None` 表示不显式声明交换机，使用默认交换机；此时 routingKey 默认为队列名。
- **路由键覆盖**：`Publish` 的 `routingKey` 参数可覆盖事件配置中的路由键，便于 Topic 多路由。
- **发布确认**：启用 PublisherConfirms 会影响发布性能，但能确保可靠投递；禁用可提升吞吐。
- **批量发布**：使用 `PublishBatch` 减少网络往返；根据消息大小调整 `BatchSize`（默认 100，建议 50-500）。
- **交换机验证**：默认启动阶段验证交换机存在且类型匹配（`ValidateExchangesOnStartup=true`）；若外部统一声明交换机，可设
  `SkipExchangeDeclare=true` 跳过声明。
- **发布背压**：启用发布确认时，未确认数量达到 `MaxOutstandingConfirms` 会等待以保护内存。
- **重试与死信**：确认 Nack 或确认超时会进入后台重试队列（指数退避 + 抖动）；超过 `RetryCount` 后写入死信存储。可通过实现
  `IDeadLetterStore` 接口自定义死信存储（如数据库、Redis 等）。

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

| 模式   | 配置                                       | 特点          | 适用场景       |
|------|------------------------------------------|-------------|------------|
| 并发执行 | `SequentialHandlerExecution = false`（默认） | 处理器并行执行，高吞吐 | 处理器之间无依赖关系 |
| 顺序执行 | `SequentialHandlerExecution = true`      | 按注册顺序依次执行   | 处理器有执行顺序依赖 |

#### 消费者中间件管道

中间件包裹整个处理器执行链路，支持事务、幂等性检查、日志记录等横切关注点。不调用 `next()` 可短路管道（如幂等性检查命中时直接返回）。

##### 定义中间件

```csharp
using EasilyNET.RabbitBus.Core.Abstraction;

public class OrderEventMiddleware(DbContext db, ILogger<OrderEventMiddleware> logger) : IEventMiddleware<OrderEvent>
{
    public async Task HandleAsync(EventContext<OrderEvent> context, Func<Task> next)
    {
        // 幂等性检查：如果已处理过该事件，直接返回（短路管道）
        if (await db.ProcessedEvents.AnyAsync(e => e.EventId == context.Event.EventId, context.CancellationToken))
        {
            logger.LogInformation("Event {EventId} already processed, skipping", context.Event.EventId);
            return; // 不调用 next()，短路管道
        }

        // 开启数据库事务包裹所有处理器
        await using var tx = await db.Database.BeginTransactionAsync(context.CancellationToken);
        try
        {
            await next(); // 执行所有处理器
            
            // 记录已处理事件
            db.ProcessedEvents.Add(new ProcessedEvent { EventId = context.Event.EventId, ProcessedAt = DateTime.UtcNow });
            await db.SaveChangesAsync(context.CancellationToken);
            await tx.CommitAsync(context.CancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(context.CancellationToken);
            throw;
        }
    }
}
```

##### 注册中间件

```csharp
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .WithMiddleware<OrderEventMiddleware>()  // 注册中间件
 .WithHandler<OrderValidationHandler>()
 .WithHandler<OrderProcessingHandler>()
 .And();
```

> 中间件注册为 Scoped 生命周期，可注入 DbContext 等 Scoped 服务。每个事件类型最多配置一个中间件。

#### 消费者回退处理器

当处理器执行过程中发生异常（Polly 重试耗尽后仍然失败）时，回退处理器被调用。返回 `ConsumerAction` 枚举决定消息的命运。

> 注意：顺序执行模式下，任一处理器失败即触发回退（后续处理器不再执行）；并发执行模式下，任一处理器失败也会触发回退。

##### 定义回退处理器

```csharp
using EasilyNET.RabbitBus.Core.Abstraction;

public class OrderFallbackHandler(ILogger<OrderFallbackHandler> logger) : IEventFallbackHandler<OrderEvent>
{
    public Task<ConsumerAction> OnFallbackAsync(OrderEvent @event, Exception exception, int retryCount)
    {
        logger.LogError(exception, "Order event {EventId} failed after {RetryCount} retries", @event.EventId, retryCount);

        // 根据异常类型决定消息处置方式
        return Task.FromResult(exception switch
        {
            // 数据验证错误：确认消费（不重试）
            ValidationException => ConsumerAction.Ack,
            // 临时性错误：重新入队稍后重试
            TimeoutException => ConsumerAction.Requeue,
            // 其他错误：发送到死信存储
            _ => ConsumerAction.DeadLetter
        });
    }
}
```

##### 注册回退处理器

```csharp
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .WithMiddleware<OrderEventMiddleware>()
 .WithFallbackHandler<OrderFallbackHandler>()  // 注册回退处理器
 .WithHandler<OrderValidationHandler>()
 .WithHandler<OrderProcessingHandler>()
 .And();
```

**ConsumerAction 枚举**：

| 值            | 说明                  |
|--------------|---------------------|
| `Ack`        | 确认消息（即使处理失败也标记为已消费） |
| `Nack`       | 拒绝消息，不重新入队          |
| `Requeue`    | 拒绝消息并重新入队，稍后重新消费    |
| `DeadLetter` | 将消息存入死信存储后确认        |

> 若未配置回退处理器，处理失败后默认 Nack（与之前行为一致）。

#### 处理器显式排序

当启用顺序执行时，可通过 `order` 参数显式控制处理器的执行顺序（值越小越先执行）：

```csharp
c.AddEvent<OrderEvent>(EModel.Routing, "order.exchange", "order.key", "order.queue")
 .ConfigureEvent(cfg => cfg.SequentialHandlerExecution = true)
 .WithHandler<OrderValidationHandler>(order: 0)    // 第一步：验证
 .WithHandler<OrderProcessingHandler>(order: 10)   // 第二步：处理
 .WithHandler<OrderNotificationHandler>(order: 20) // 第三步：通知
 .And();
```

> 不指定 `order` 时默认为 0，按注册顺序执行（与之前行为一致）。

#### 自定义弹性管道

每个事件可配置独立的 Polly 弹性管道，覆盖全局默认策略：

```csharp
c.AddEvent<CriticalEvent>(EModel.Routing, "critical.exchange", "critical.key", "critical.queue")
 .WithHandlerResilience(builder =>
 {
     // 关键业务：更多重试、更长超时
     builder.AddRetry(new()
     {
         MaxRetryAttempts = 5,
         Delay = TimeSpan.FromSeconds(2),
         BackoffType = DelayBackoffType.Exponential,
         UseJitter = true
     });
     builder.AddTimeout(TimeSpan.FromMinutes(2));
 })
 .WithHandler<CriticalEventHandler>()
 .And();

c.AddEvent<LogEvent>(EModel.Routing, "log.exchange", "log.key", "log.queue")
 .WithHandlerResilience(builder =>
 {
     // 日志事件：快速失败，不重试
     builder.AddTimeout(TimeSpan.FromSeconds(5));
 })
 .WithHandler<LogEventHandler>()
 .And();
```

> 未配置自定义弹性管道时，使用全局默认的 HandlerPipeline。

#### OpenTelemetry 分布式追踪

框架内置 `System.Diagnostics.ActivitySource` 支持，自动为发布和消费操作创建追踪 span，并通过 RabbitMQ 消息头传播 trace
context（`traceparent`/`tracestate`）。

##### 接入 OpenTelemetry

```csharp
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("MyApp"))
    .WithTracing(tracing =>
    {
        tracing.AddSource("EasilyNET.RabbitBus")  // 添加 RabbitBus ActivitySource
               .AddAspNetCoreInstrumentation()
               .AddOtlpExporter();
    });
```

##### 追踪标签

发布 span（`rabbitmq.publish`，`ActivityKind.Producer`）：

| 标签                               | 说明         |
|----------------------------------|------------|
| `messaging.system`               | `rabbitmq` |
| `messaging.destination`          | 交换机名称      |
| `messaging.destination_kind`     | `exchange` |
| `messaging.rabbitmq.routing_key` | 路由键        |
| `messaging.message.id`           | 事件 ID      |

消费 span（`rabbitmq.consume`，`ActivityKind.Consumer`）：

| 标签                      | 说明         |
|-------------------------|------------|
| `messaging.system`      | `rabbitmq` |
| `messaging.source`      | 来源交换机      |
| `messaging.destination` | 路由键        |
| `messaging.consumer_id` | 消费者索引      |

> 发布端自动将 `traceparent`/`tracestate` 注入消息头，消费端自动提取并关联为父级 span，实现跨进程的完整链路追踪。

#### 自定义死信存储

`IDeadLetterStore` 是公开接口，可实现自定义死信存储（如 Redis、数据库等）：

```csharp
// ✅ 可复制使用的 Redis 死信存储示例（同时也是一个模板）：
// - 该实现会把死信消息序列化后存入 Redis String
// - 读取时会根据 OriginalEventType 反序列化回具体事件类型（要求事件类型在当前应用可加载）
// - 注意：示例中使用 server.KeysAsync(pattern) 扫描 key，生产环境建议改为 Set/SortedSet + Scan 维护索引
//
// NuGet:
// - StackExchange.Redis

using System.Runtime.CompilerServices;
using System.Text.Json;
using EasilyNET.RabbitBus.Abstractions;
using EasilyNET.RabbitBus.Core.Abstraction;
using StackExchange.Redis;

/// <summary>
/// Redis 死信存储示例
/// </summary>
public class RedisDeadLetterStore : IDeadLetterStore
{
    private readonly IConnectionMultiplexer _redis;
    private readonly JsonSerializerOptions _jsonOptions;

    public RedisDeadLetterStore(IConnectionMultiplexer redis, JsonSerializerOptions? jsonOptions = null)
    {
        _redis = redis;
        _jsonOptions = jsonOptions ?? new(JsonSerializerDefaults.Web);
    }

    private const string KeyPrefix = "deadletter:";

    private static string BuildKey(string eventType, string eventId) => $"{KeyPrefix}{eventType}:{eventId}";

    private sealed record DeadLetterEnvelope(
        string EventType,
        string EventId,
        DateTime CreatedUtc,
        int RetryCount,
        string OriginalEventType,
        string OriginalEventJson);

    private sealed class RedisDeadLetterMessage(string eventType, string eventId, DateTime createdUtc, int retryCount, IEvent originalEvent) : IDeadLetterMessage
    {
        public string EventType { get; } = eventType;
        public string EventId { get; } = eventId;
        public DateTime CreatedUtc { get; } = createdUtc;
        public int RetryCount { get; } = retryCount;
        public IEvent OriginalEvent { get; } = originalEvent;
    }

    public async ValueTask StoreAsync(IDeadLetterMessage message, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        var originalEventType = message.OriginalEvent.GetType().AssemblyQualifiedName;
        if (string.IsNullOrWhiteSpace(originalEventType))
        {
            throw new InvalidOperationException("OriginalEvent type must have a valid AssemblyQualifiedName.");
        }

        var envelope = new DeadLetterEnvelope(
            message.EventType,
            message.EventId,
            message.CreatedUtc,
            message.RetryCount,
            originalEventType,
            JsonSerializer.Serialize(message.OriginalEvent, message.OriginalEvent.GetType(), _jsonOptions));

        var key = BuildKey(message.EventType, message.EventId);
        var value = JsonSerializer.Serialize(envelope, _jsonOptions);
        await db.StringSetAsync(key, value).ConfigureAwait(false);
    }

    public async IAsyncEnumerable<IDeadLetterMessage> GetAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*").WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested) yield break;
            var value = await db.StringGetAsync(key);
            if (value.HasValue)
            {
                var envelope = JsonSerializer.Deserialize<DeadLetterEnvelope>(value!, _jsonOptions);
                if (envelope is null)
                {
                    continue;
                }

                var evtType = Type.GetType(envelope.OriginalEventType);
                if (evtType is null)
                {
                    continue;
                }

                var evt = JsonSerializer.Deserialize(envelope.OriginalEventJson, evtType, _jsonOptions) as IEvent;
                if (evt is null)
                {
                    continue;
                }

                yield return new RedisDeadLetterMessage(envelope.EventType, envelope.EventId, envelope.CreatedUtc, envelope.RetryCount, evt);
            }
        }
    }

    public async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        await foreach (var key in server.KeysAsync(pattern: $"{KeyPrefix}*").WithCancellation(cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            await db.KeyDeleteAsync(key).ConfigureAwait(false);
        }
    }
}

// 注册自定义死信存储（替换内置的 InMemoryDeadLetterStore）
builder.Services.AddSingleton<IDeadLetterStore, RedisDeadLetterStore>();
```

**IDeadLetterMessage 接口成员**：

| 属性              | 类型         | 说明          |
|-----------------|------------|-------------|
| `EventType`     | `string`   | 事件类型名称      |
| `EventId`       | `string`   | 事件唯一标识      |
| `CreatedUtc`    | `DateTime` | 消息创建时间（UTC） |
| `RetryCount`    | `int`      | 进入死信前的重试次数  |
| `OriginalEvent` | `IEvent`   | 原始事件实例      |

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
            - 发布: `rabbitmq.publish.normal.total`, `rabbitmq.publish.retried.total`,
              `rabbitmq.publish.discarded.total`
            - 确认: `rabbitmq.publish.confirm.ack.total`, `rabbitmq.publish.confirm.nack.total`,
              `rabbitmq.publish.outstanding.confirms`
            - 重试: `rabbitmq.retry.enqueued.total`
            - 连接: `rabbitmq.connection.reconnects.total`, `rabbitmq.connection.active`, `rabbitmq.channel.active`,
              `rabbitmq.connection.state`
            - 死信: `rabbitmq.deadletter.total`
          > 说明（EN）：These are the latest dot-separated metric names. Older versions used underscore-based names (for
          example: `rabbitmq_published_normal_total`). If you previously collected metrics via the old names, please
          update your dashboards/alerts accordingly.
          > 说明（中文）：以上为最新的点分式指标命名规范，已从旧的下划线风格（例如：`rabbitmq_published_normal_total`
          ）迁移而来。如你已基于旧名称配置监控/告警，请同步更新对应配置。

    - 快速观察(开发):

  ```bash
  dotnet-counters monitor --process <your-app-pid> --counters EasilyNET.RabbitBus
  ```

- OpenTelemetry: 按常规方式接入 OTLP/Prometheus 导出器即可收集上述指标。
- 分布式追踪: ActivitySource 名称为 `EasilyNET.RabbitBus`，支持发布/消费 span 自动创建与 trace context 传播。

  ```csharp
  // 接入 OpenTelemetry 追踪
  builder.Services.AddOpenTelemetry()
      .WithTracing(tracing => tracing.AddSource("EasilyNET.RabbitBus"));
  ```

#### 发布限流/背压

- 当启用发布确认(PublisherConfirms=true)时,框架会以 `MaxOutstandingConfirms` 为阈值控制未确认发布数量。
- 若达到阈值,发布线程将进行短暂等待,直到确认数下降,以防止内存暴涨或确认集合过大。
- 建议根据发布速率与确认延迟进行压测,选择合适的阈值(默认 1000, 常见范围 500~5000)。

#### 交换机声明与验证

- `SkipExchangeDeclare=true` 时,框架不会主动声明交换机,仅在需要时进行被动验证或直接发布(取决于场景)。
- `ValidateExchangesOnStartup=true` 时,启动阶段会被动(passive)验证交换机是否存在且类型匹配;
  若类型不一致,会明确报错并终止启动(避免运行期频繁连接被关闭)。
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

#### 配置参数参考表

| 配置方法                     | 参数                                  | 默认值                      | 说明                            |
|--------------------------|-------------------------------------|--------------------------|-------------------------------|
| `WithConnection`         | -                                   | -                        | RabbitMQ 连接配置(主机、端口、认证等)      |
| `WithConsumerSettings`   | `dispatchConcurrency`               | 10                       | 消费者调度并发数,控制同时处理的消息数           |
|                          | `prefetchCount`                     | 100                      | QoS 预取计数,限制未确认消息数量            |
|                          | `prefetchSize`                      | 0                        | QoS 预取大小                      |
|                          | `global`                            | false                    | QoS 是否全局                      |
|                          | `consumerChannelLimit`              | 0                        | 消费者通道上限(0 表示不限制)              |
| `WithResilience`         | `retryCount`                        | 5                        | 发布失败/确认失败的重试次数                |
|                          | `retryIntervalSeconds`              | 1                        | 后台重试检查间隔(秒)                   |
|                          | `publisherConfirms`                 | true                     | 是否启用发布确认模式                    |
|                          | `maxOutstandingConfirms`            | 1000                     | 最大未确认发布数量                     |
|                          | `batchSize`                         | 100                      | 批量发布大小                        |
|                          | `confirmTimeoutMs`                  | 30000                    | 发布确认超时时间(毫秒)                  |
| `WithExchangeSettings`   | `skipExchangeDeclare`               | false                    | 跳过交换机声明(外部已声明时可启用)            |
|                          | `validateExchangesOnStartup`        | false                    | 启动验证交换机类型(调用该方法时默认)           |
| `WithRetryQueueSizing`   | `maxSize`                           | -                        | 固定最大重试队列长度(>0 生效)             |
|                          | `memoryRatio`                       | 0.02                     | 估算队列内存占比(0-0.25)              |
|                          | `avgEntryBytes`                     | 2048                     | 单条重试项估算字节数                    |
| `WithApplication`        | `appName`                           | `MachineName, EasilyNET` | 应用标识,用于日志与指标标签                |
| `WithEventQos`           | `prefetchCount`                     | -                        | 事件级 QoS 设置(覆盖全局设置)            |
|                          | `prefetchSize`                      | -                        | 事件级 QoS 预取大小                  |
|                          | `global`                            | -                        | 事件级 QoS 是否全局                  |
| `WithEventHeaders`       | `headers`                           | -                        | 消息头参数                         |
| `WithEventQueueArgs`     | `args`                              | -                        | 队列声明参数(x-max-priority 等)      |
| `WithEventExchangeArgs`  | `args`                              | -                        | 交换机声明参数                       |
| `WithBindingArguments`   | `arguments`                         | -                        | Headers交换机绑定参数(x-match及匹配键值对) |
| `WithHandler`            | `THandler`                          | -                        | 注册事件处理器(必须)                   |
|                          | `order`                             | 0                        | 处理器执行顺序(值越小越先执行)              |
| `WithMiddleware`         | `TMiddleware`                       | -                        | 注册事件中间件(可选,每事件最多一个)           |
| `WithFallbackHandler`    | `TFallback`                         | -                        | 注册回退处理器(可选,重试耗尽后调用)           |
| `WithHandlerResilience`  | `Action<ResiliencePipelineBuilder>` | 全局 HandlerPipeline       | 自定义事件级弹性管道                    |
| `WithHandlerThreadCount` | `threadCount`                       | 1                        | 该事件消费者数量(并行度)                 |
| `ConfigureEvent`         | `SequentialHandlerExecution`        | false                    | 是否按顺序执行处理器                    |
|                          | `Exchange/Queue/Qos/Headers/...`    | -                        | 事件高级配置入口                      |
| `IgnoreHandler`          | -                                   | -                        | 忽略指定的处理器                      |
| `WithSerializer`         | -                                   | System.Text.Json         | 自定义消息序列化器                     |
| 全局                       | `SkipExchangeDeclare`               | false                    | 跳过交换机声明(外部已声明时可启用)            |
| 全局                       | `ValidateExchangesOnStartup`        | false                    | 启动阶段验证交换机类型与存在性               |

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
- **HandlerPipeline**：消息处理器的重试、超时策略（可通过 `WithHandlerResilience` 按事件覆盖）

##### 消费者中间件管道

消费侧采用中间件管道模式处理消息：

```
消息到达 → 反序列化 → [中间件 HandleAsync] → 处理器链路（顺序/并发） → Ack
                              ↓ (异常)
                     [回退处理器 OnFallbackAsync] → ConsumerAction → Ack/Nack/Requeue/DeadLetter
```

- **中间件**（`IEventMiddleware<T>`）：包裹整个处理器链路，支持事务、幂等性、日志等
- **回退处理器**（`IEventFallbackHandler<T>`）：Polly 重试耗尽后调用，决定消息命运
- **处理器排序**：通过 `HandlerConfiguration.Order` 控制顺序执行时的处理器顺序

##### OpenTelemetry 追踪

框架使用 `System.Diagnostics.ActivitySource`（名称：`EasilyNET.RabbitBus`）实现分布式追踪：

- **发布端**：创建 `rabbitmq.publish` Producer span，将 `traceparent`/`tracestate` 注入消息头
- **消费端**：从消息头提取父级 trace context，创建 `rabbitmq.consume` Consumer span
- **跨进程关联**：发布和消费 span 自动通过消息头关联，在 Jaeger/Zipkin 等工具中可查看完整链路

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

---

# EasilyNET.RabbitBus 套件 · 完整使用文档与场景选型指南

> 本节是对 `EasilyNET.RabbitBus`、`EasilyNET.RabbitBus.Core` 两个包的**统一汇总**，侧重「有哪些能力 / 什么场景该用哪个 / 怎么选」。
> 上文已给出每个功能的细粒度用法与完整配置参考表，本节聚焦**全局视角与选型决策**，便于快速上手与排错。

---

## 目录（套件总览）

- [1. 套件结构与选型](#1-套件结构与选型)
- [2. 交换机模式（EModel）选型](#2-交换机模式emodel选型)
- [3. 功能 → 场景速查表](#3-功能--场景速查表)
- [4. 最小可用配置（5 分钟上手）](#4-最小可用配置5-分钟上手)
- [5. 核心抽象一览（Core 包）](#5-核心抽象一览core-包)
- [6. 发布 API（IBus）](#6-发布-apiibus)
- [7. 消费链路：中间件 / 处理器 / 回退](#7-消费链路中间件--处理器--回退)
- [8. 可靠性：发布确认、重试、死信](#8-可靠性发布确认重试死信)
- [9. 配置预设（按场景套用）](#9-配置预设按场景套用)
- [10. 可观测性：指标与追踪](#10-可观测性指标与追踪)
- [11. 部署前提与环境矩阵](#11-部署前提与环境矩阵)
- [12. 常见问题排查](#12-常见问题排查)

---

## 1. 套件结构与选型

| 包 | 角色 | 关键依赖 | 何时引用 |
|---|---|---|---|
| **EasilyNET.RabbitBus.Core** | 契约层：`IEvent` / `Event`、`IEventHandler` / `IEventMiddleware` / `IEventFallbackHandler`、`IBus`、`IBusSerializer`、`EModel`、`ConsumerAction`、`EventContext` | 无（纯抽象） | 把**事件消息定义**独立成库、供生产者与消费者共享时单独引用，避免重复声明、减少传递依赖。 |
| **EasilyNET.RabbitBus** | 实现层：DI 注册、流式配置、连接/发布/消费/重试/死信、健康检查、指标、OpenTelemetry | `RabbitMQ.Client`、`Microsoft.Extensions.*`、`Microsoft.Extensions.Resilience`(Polly)、`Core` | 真正要收发消息的进程（API / Worker / 控制台 / 通用 Host）。**主程序装这个即可。** |

- 目标框架：`net10.0`、`net11.0`。
- `EasilyNET.RabbitBus` 已不再依赖 ASP.NET Core 共享框架（由 `EasilyNET.RabbitBus.AspNetCore` 重命名而来），可用于任意 Host。
- ⚠️ **延迟消息已移除**：不再支持 `rabbitmq-delayed-message-exchange` 插件（官方已于 2026-01-29 停止维护）。延迟/重试请改用 DLX + TTL 或外部调度器（详见上文「关于延迟消息功能的移除说明」）。

```bash
dotnet add package EasilyNET.RabbitBus        # 主包（含 Core）
# 仅共享事件契约的项目（如领域模型库）可只引用：
dotnet add package EasilyNET.RabbitBus.Core
```

**典型分层**：领域/契约库引用 `Core` 定义 `XxxEvent : Event`；生产者与消费者进程引用 `EasilyNET.RabbitBus`，各自注册同一事件即可互通。

---

## 2. 交换机模式（EModel）选型

`AddEvent<T>(EModel, exchangeName, routingKey, queueName)` 的第一个参数决定路由语义：

| EModel | RabbitMQ 交换机 | 路由依据 | 适用场景 |
|---|---|---|---|
| `None` | 默认交换机 | routingKey 默认取队列名 | 最简单的点对点：一个事件→一个队列，无需声明交换机 |
| `Routing` | `direct` | routingKey 精确匹配 | 定向投递：按 key 路由到指定队列（最常用） |
| `Topics` | `topic` | routingKey 模式匹配（`order.*` / `user.#`） | 灵活路由：一类消息多种细分（如 `order.created` / `order.paid`），生产者可在 `Publish` 时覆盖 routingKey |
| `PublishSubscribe` | `fanout` | 忽略 routingKey，广播 | 一条消息多方消费：通知广播、缓存失效、扇出 |
| `Headers` | `headers` | 消息头 + 绑定参数（`x-match=all/any`）匹配 | 多维度内容过滤（如 `format=pdf & type=report`），不依赖 routingKey |

> `Headers` 模式性能略低于 direct/topic（逐键匹配），仅在确需多维过滤时使用。

---

## 3. 功能 → 场景速查表

| 你的需求 | 用什么 | 入口 |
|---|---|---|
| 定义事件 | 继承 `Event`（自带雪花 `EventId`）或实现 `IEvent` | Core 包 |
| 消费事件 | 实现 `IEventHandler<T>` | `.WithHandler<H>()` |
| 一个事件多个处理器 | 注册多个 Handler | 多次 `.WithHandler<H>()` |
| 处理器顺序执行 | `SequentialHandlerExecution = true` + `order` | `.ConfigureEvent(...)` / `.WithHandler<H>(order)` |
| 提高单事件消费并行度 | 多消费者通道 | `.WithHandlerThreadCount(n)` |
| 事务 / 幂等 / 审计（横切） | 中间件（可短路 `next()`） | `.WithMiddleware<M>()` |
| 重试耗尽后自定义处置 | 回退处理器返回 `ConsumerAction` | `.WithFallbackHandler<F>()` |
| 单事件独立重试/超时策略 | Polly 弹性管道 | `.WithHandlerResilience(b => ...)` |
| 跳过某个已注册处理器 | 忽略 | `c.IgnoreHandler<T, H>()` |
| 优先级队列 | 队列参数 `x-max-priority` + 发布 `priority` | `.WithEventQueueArgs(...)` |
| 事件级 QoS（预取） | 覆盖全局 | `.WithEventQos(prefetchCount)` |
| 自定义序列化（MsgPack 等） | 实现 `IBusSerializer` | `c.WithSerializer<S>()` |
| 批量发布提吞吐 | 批量 API | `bus.PublishBatch(events)` |
| 可靠投递不丢消息 | 发布确认 + 背压 | `c.WithResilience(publisherConfirms: true)` |
| 失败消息落库/转存 | 自定义死信存储 | 实现 `IDeadLetterStore` 并注册 |
| 健康检查 | 已自动注册 | `app.MapHealthChecks("/health")` |
| 指标 / 链路追踪 | Meter & ActivitySource：`EasilyNET.RabbitBus` | OpenTelemetry `AddSource/AddMeter` |

---

## 4. 最小可用配置（5 分钟上手）

```csharp
// 1) 事件（Core 包）：自带雪花 EventId
public class TestEvent : Event { public string Message { get; set; } = default!; }

// 2) 处理器
public class TestEventHandler(ILogger<TestEventHandler> logger) : IEventHandler<TestEvent>
{
    public Task HandleAsync(TestEvent e)
    {
        logger.LogInformation("收到: {msg}", e.Message);
        return Task.CompletedTask;
    }
}

// 3) 注册
builder.Services.AddRabbitBus(c =>
{
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!));
    c.AddEvent<TestEvent>(EModel.Routing, "test.exchange", "test.key", "test.queue")
     .WithHandler<TestEventHandler>()
     .And();
});

// 4) 发布（注入 IBus）
public class FooService(IBus bus)
{
    public Task SendAsync() => bus.Publish(new TestEvent { Message = "hello" });
}
```

> ⚠️ 只有通过 `.WithHandler<H>()` **显式注册**的处理器才会创建消费者并注入 DI。Handler/Middleware/Fallback 均为 **Scoped** 生命周期（每条消息独立作用域，可安全注入 `DbContext`）。

---

## 5. 核心抽象一览（Core 包）

| 类型 | 作用 | 关键成员 |
|---|---|---|
| `IEvent` / `Event` | 事件契约 / 默认实现 | `string EventId`（雪花 ID，可用于幂等/关联） |
| `IEventHandler<in TEvent>` | 消费者 | `Task HandleAsync(TEvent e)` |
| `IEventMiddleware<TEvent>` | 处理器链中间件 | `Task HandleAsync(EventContext<TEvent> ctx, Func<Task> next)` |
| `IEventFallbackHandler<in TEvent>` | 重试耗尽后的兜底 | `Task<ConsumerAction> OnFallbackAsync(TEvent e, Exception ex, int retryCount)` |
| `EventContext<TEvent>` | 中间件上下文 | `Event` / `Headers` / `CancellationToken` |
| `ConsumerAction` | 消息处置枚举 | `Ack` / `Nack` / `Requeue` / `DeadLetter` |
| `IBus` | 发布入口 | `Publish` / `PublishBatch` / `Publish(object, Type, ...)` |
| `IBusSerializer` | 序列化器 | `byte[] Serialize(...)` / `object? Deserialize(...)` |
| `EModel` | 交换机模式 | 见上表 |

---

## 6. 发布 API（IBus）

```csharp
// 单条（按事件配置路由）
await bus.Publish(new TestEvent { Message = "normal" });

// 覆盖 routingKey（Topic 多路由生产者）
await bus.Publish(new TestEvent { Message = "t" }, routingKey: "order.paid");

// 优先级（需队列声明 x-max-priority，推荐 0-9）
await bus.Publish(new TestEvent { Message = "p" }, priority: 5);

// 逐条消息头（合并并覆盖事件静态 Headers）
await bus.Publish(e, headers: new Dictionary<string, object?> { ["x-tenant"] = "t1" });

// 批量（减少网络往返；按消息大小调 BatchSize，默认 100）
await bus.PublishBatch(events);

// 非泛型（运行时动态类型）
await bus.Publish(obj, typeof(TestEvent));
```

> 未注册或被禁用的事件：发布时记录警告并直接返回（不抛异常）。

---

## 7. 消费链路：中间件 / 处理器 / 回退

```text
消息到达 → 反序列化 → [中间件 HandleAsync] → 处理器链（顺序/并发） → Ack
                          ↓ (重试耗尽仍异常)
                 [回退处理器 OnFallbackAsync] → ConsumerAction → Ack/Nack/Requeue/DeadLetter
```

- **处理器执行模式**：默认并发（高吞吐）；`SequentialHandlerExecution = true` 时按 `order` 顺序依次执行（值越小越先）。
- **中间件**：包裹整条处理器链，不调用 `next()` 即短路（幂等命中直接返回）。每事件最多一个；DI 解析失败会抛 `InvalidOperationException`（不静默跳过）。
- **回退处理器**：Polly 重试耗尽后调用，按异常类型返回 `ConsumerAction` 决定消息命运；未配置时默认 Nack。

详见上文「消费者中间件管道」「消费者回退处理器」「处理器显式排序」「自定义弹性管道」章节的完整示例。

---

## 8. 可靠性：发布确认、重试、死信

| 机制 | 说明 | 关键配置 |
|---|---|---|
| 发布确认（Publisher Confirms） | 等待 broker ack，确保投递；按 seq 跟踪、超时 nack | `WithResilience(publisherConfirms: true, confirmTimeoutMs: 30000)` |
| 发布背压 | 未确认数达阈值时短暂等待，防内存暴涨 | `maxOutstandingConfirms`（默认 1000，常见 500~5000） |
| 后台重试 | Nack/确认超时进入重试队列，指数退避 + 抖动（1s→30s 封顶） | `retryCount`、`retryIntervalSeconds` |
| 死信 | 超过 `retryCount` 后写入死信存储 | 内置 `InMemoryDeadLetterStore`；自定义实现 `IDeadLetterStore` 并 `AddSingleton` 覆盖 |
| 重试队列容量 | 固定上限或按内存动态估算 | `WithRetryQueueSizing(maxSize, memoryRatio, avgEntryBytes)` |

`IDeadLetterMessage` 成员：`EventType` / `EventId` / `CreatedUtc` / `RetryCount` / `OriginalEvent`（可重放）。
自定义死信存储（Redis 等）完整模板见上文「自定义死信存储」章节。

---

## 9. 配置预设（按场景套用）

```csharp
// 高吞吐（日志、埋点、可容忍极少丢失）
c.WithConsumerSettings(dispatchConcurrency: 50, prefetchCount: 200);
c.WithResilience(retryCount: 3, publisherConfirms: false);   // 关确认换吞吐
// 事件级：.WithEventQos(prefetchCount: 100).WithHandlerThreadCount(8)

// 高可靠（金融、订单、关键业务）
c.WithConsumerSettings(dispatchConcurrency: 5, prefetchCount: 20);
c.WithResilience(retryCount: 10, publisherConfirms: true);   // 开确认保不丢
// 事件级：.WithEventQos(prefetchCount: 10).WithHandlerThreadCount(1)

// 外部已统一声明交换机（IaC/运维侧）
c.WithExchangeSettings(skipExchangeDeclare: true, validateExchangesOnStartup: false);

// 集群连接（多节点用 ConnectionFactory，DDNS/集群更稳）
c.WithConnection(f => { f.HostName = "rabbitmq-cluster"; f.UserName = "user"; f.Password = "pwd"; f.Port = 5672; f.VirtualHost = "/"; });
```

> `WithExchangeSettings()` 一旦调用且不传参，`validateExchangesOnStartup` 即变为 `false`，注意按需显式传 `true`。

---

## 10. 可观测性：指标与追踪

```csharp
// 指标 Meter 名 & 追踪 ActivitySource 名 均为 "EasilyNET.RabbitBus"
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("EasilyNET.RabbitBus"))     // 发布/消费 span，自动透传 traceparent
    .WithMetrics(m => m.AddMeter("EasilyNET.RabbitBus"));
```

关键指标（点分式命名，旧下划线命名已弃用）：

| 类别 | 指标 |
|---|---|
| 发布 | `rabbitmq.publish.normal.total`、`rabbitmq.publish.retried.total`、`rabbitmq.publish.discarded.total` |
| 确认 | `rabbitmq.publish.confirm.ack.total`、`rabbitmq.publish.confirm.nack.total`、`rabbitmq.publish.outstanding.confirms` |
| 重试/死信 | `rabbitmq.retry.enqueued.total`、`rabbitmq.deadletter.total` |
| 连接 | `rabbitmq.connection.reconnects.total`、`rabbitmq.connection.active`、`rabbitmq.channel.active`、`rabbitmq.connection.state` |

```bash
# 开发期快速观察
dotnet-counters monitor --process <pid> --counters EasilyNET.RabbitBus
```

健康检查 `RabbitBusHealthCheck` 已自动注册：通道开启=Healthy，存在但关闭=Degraded，获取失败=Unhealthy。

---

## 11. 部署前提与环境矩阵

| 能力 | 要求 |
|---|---|
| 基础收发 / direct / topic / fanout / headers | RabbitMQ 3.x+（单点或集群均可） |
| 优先级队列 | 队列声明 `x-max-priority` |
| 发布确认 / 背压 | 无特殊要求（开 `publisherConfirms`） |
| 集群高可用 | 多节点 + 镜像/Quorum 队列（运维侧配置） |
| 延迟消息 | ❌ 已移除插件支持，改用 DLX + TTL 或外部调度器 |

连接配置支持：连接串 `Uri`、单点 `HostName/Port/...`、或多端点 `AmqpTcpEndpoints`（集群/DDNS 更稳）。

---

## 12. 常见问题排查

| 现象 | 可能原因 / 解决 |
|---|---|
| 连接失败 | 检查连接串格式、RabbitMQ 运行状态、用户名/密码/虚拟主机权限 |
| 处理器不触发 | 是否用 `.WithHandler<H>()` **显式注册**；事件是否 `Enabled`；交换机/队列/绑定是否正确 |
| 消息丢失 | 开启 `publisherConfirms`；确认消费端正确 Ack；交换机/队列已正确声明 |
| 启动报交换机类型不匹配 | `ValidateExchangesOnStartup=true` 时被动校验失败；修正交换机类型或外部统一声明并 `SkipExchangeDeclare=true` |
| 注入 Singleton 状态丢失 | Handler/Middleware/Fallback 已改为 **Scoped**；共享状态请注入独立的 Singleton 服务 |
| 吞吐不足 | 提高 `dispatchConcurrency`、`prefetchCount`、`WithHandlerThreadCount`；高吞吐可关闭 `publisherConfirms` |
| 内存增长 | 合理设置 `prefetchCount` 与 `maxOutstandingConfirms`；监控队列积压；调小重试队列容量 |
| 想要延迟投递 | 插件已移除：用死信交换机（DLX）+ TTL，或外部调度器 + 持久化存储 |
