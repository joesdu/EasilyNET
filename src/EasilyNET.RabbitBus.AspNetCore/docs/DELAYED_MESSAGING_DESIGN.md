# RabbitMQ 延迟消息实现方案：DLX + TTL 替代方案设计文档

> 本文档描述了使用死信交换机（DLX）+ TTL 组合实现延迟消息的详细设计方案，用于替代已停止维护的 `rabbitmq-delayed-message-exchange` 插件。

## 一、架构概述

### 1.1 DLX + TTL 工作原理

RabbitMQ 的延迟消息可通过 **死信交换机（Dead Letter Exchange, DLX）** 与 **消息 TTL（Time-To-Live）** 组合实现：

```
发布延迟消息 → 延迟队列(设置TTL) → TTL过期 → DLX路由 → 目标队列 → 消费者处理
```

**核心机制**：
1. 消息发布到**延迟队列**（无消费者，仅用于暂存）
2. 队列或消息设置 TTL，到期后消息变为"死信"
3. 死信自动路由到配置的 DLX（死信交换机）
4. DLX 将消息路由到**目标队列**，业务消费者从此队列消费

---

## 二、设计方案对比

### 方案 A：单队列 + 消息级 TTL（Per-Message TTL）

**拓扑结构**：
```
[延迟交换机] → [单个延迟队列] --DLX--> [目标交换机] → [目标队列]
                ↑ x-message-ttl=null
                ↑ x-dead-letter-exchange=目标交换机
```

**优点**：
- ✅ **灵活性极高**：支持任意延迟时长（1ms ~ 数天）
- ✅ **配置简单**：只需一个延迟队列

**缺点**：
- ❌ **严重的队头阻塞问题**：RabbitMQ 只检查队列头部消息的 TTL
- ❌ **消息乱序**：先发布的长延迟消息会阻塞后发布的短延迟消息
- ❌ **不适合生产环境**

---

### 方案 B：分层延迟队列（Tiered Delay Queues）

**拓扑结构**：
```
[延迟交换机(topic)] 
    ├─ delay.1s   → [延迟队列-1s]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.5s   → [延迟队列-5s]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.30s  → [延迟队列-30s] --DLX--> [目标交换机] → [目标队列]
    ├─ delay.1m   → [延迟队列-1m]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.5m   → [延迟队列-5m]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.30m  → [延迟队列-30m] --DLX--> [目标交换机] → [目标队列]
    └─ delay.1h   → [延迟队列-1h]  --DLX--> [目标交换机] → [目标队列]
```

**优点**：
- ✅ **无队头阻塞**：队列级 TTL 保证所有消息同时过期
- ✅ **性能稳定**：每个队列的 TTL 固定
- ✅ **适合大规模场景**

**缺点**：
- ❌ **精度损失**：只能使用预定义的延迟档位
- ❌ **拓扑复杂**：需要创建和维护多个延迟队列

---

### 方案 C：混合方案（推荐）★★★★★

**核心思想**：结合方案 A 和方案 B 的优点，根据延迟时长动态选择策略。

**拓扑结构**：
```
[延迟交换机(topic)]
    ├─ delay.precise → [精确延迟队列] --DLX--> [目标交换机] → [目标队列]
    │                   ↑ 消息级TTL，用于短延迟（<10s）
    │
    ├─ delay.1s   → [延迟队列-1s]   --DLX--> [目标交换机] → [目标队列]
    ├─ delay.5s   → [延迟队列-5s]   --DLX--> [目标交换机] → [目标队列]
    ├─ delay.30s  → [延迟队列-30s]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.1m   → [延迟队列-1m]   --DLX--> [目标交换机] → [目标队列]
    ├─ delay.5m   → [延迟队列-5m]   --DLX--> [目标交换机] → [目标队列]
    ├─ delay.30m  → [延迟队列-30m]  --DLX--> [目标交换机] → [目标队列]
    ├─ delay.1h   → [延迟队列-1h]   --DLX--> [目标交换机] → [目标队列]
    ├─ delay.6h   → [延迟队列-6h]   --DLX--> [目标交换机] → [目标队列]
    └─ delay.24h  → [延迟队列-24h]  --DLX--> [目标交换机] → [目标队列]
```

**路由策略**：
```csharp
延迟时长 < 10秒        → 精确延迟队列（消息级TTL）
10秒 ≤ 延迟时长 < 24小时 → 分层延迟队列（队列级TTL，向上取整）
延迟时长 ≥ 24小时      → 拒绝或降级处理（记录到数据库，外部调度器处理）
```

**优点**：
- ✅ **兼顾精度与性能**：短延迟精确，长延迟高效
- ✅ **避免队头阻塞**：精确队列仅用于短延迟（<10s），阻塞影响可控
- ✅ **覆盖常见场景**：90% 的延迟消息需求在 24 小时内
- ✅ **资源可控**：队列数量有限（约 10 个）

---

## 三、推荐实现方案（方案 C）

### 3.1 延迟档位设计

| 档位名称 | 延迟时长 | 队列级TTL | 适用范围 |
|---------|---------|----------|---------|
| `precise` | 动态 | 无（消息级） | 0-10s |
| `1s` | 1秒 | 1000ms | - |
| `5s` | 5秒 | 5000ms | 2-10s |
| `30s` | 30秒 | 30000ms | 10-60s |
| `1m` | 1分钟 | 60000ms | 30s-3m |
| `5m` | 5分钟 | 300000ms | 3-10m |
| `30m` | 30分钟 | 1800000ms | 10-60m |
| `1h` | 1小时 | 3600000ms | 30m-3h |
| `6h` | 6小时 | 21600000ms | 3-12h |
| `24h` | 24小时 | 86400000ms | 12-24h |

### 3.2 档位选择算法

```csharp
private static readonly long[] DelayTiers = 
{
    1000,      // 1s
    5000,      // 5s
    30000,     // 30s
    60000,     // 1m
    300000,    // 5m
    1800000,   // 30m
    3600000,   // 1h
    21600000,  // 6h
    86400000   // 24h
};

private static string SelectDelayQueue(TimeSpan delay, string eventTypeName)
{
    var delayMs = (long)delay.TotalMilliseconds;
    
    // 零延迟或负延迟拒绝（与 4.1 节边界策略一致）
    if (delayMs <= 0)
        throw new ArgumentException("Delay must be positive.", nameof(delay));
    
    // 短延迟使用精确队列
    if (delayMs < 10000) return $"delay.precise.{eventTypeName}";
    
    // 超长延迟拒绝
    if (delayMs > 86400000) 
        throw new ArgumentException("Delay exceeds 24 hours. Use external scheduler.");
    
    // 选择最接近的档位（向上取整）
    foreach (var tier in DelayTiers)
    {
        if (delayMs <= tier) return $"delay.{FormatTier(tier)}.{eventTypeName}";
    }
    
    return $"delay.24h.{eventTypeName}";
}
```

### 3.3 配置 API 设计

```csharp
// Program.cs
builder.Services.AddRabbitBus(c =>
{
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!));
    
    // 启用延迟消息功能
    c.WithDelayedMessaging(options =>
    {
        // 延迟交换机名称（默认：easilynet.delay，为普通 direct 交换机，与延迟插件无关）
        options.DelayExchangeName = "easilynet.delay";
        
        // 精确延迟阈值（默认：10秒）
        options.PreciseDelayThreshold = TimeSpan.FromSeconds(10);
        
        // 最大延迟时长（默认：24小时）
        options.MaxDelay = TimeSpan.FromHours(24);
        
        // 是否自动创建延迟队列（默认：true）
        options.AutoDeclareDelayQueues = true;
    });
    
    // 事件配置
    c.AddEvent<OrderTimeoutEvent>(EModel.Routing, "order.exchange", "order.timeout", "order.timeout.queue")
     .WithHandler<OrderTimeoutHandler>();
});
```

### 3.4 发布延迟消息 API

```csharp
public interface IBus
{
    // 现有方法
    Task Publish<T>(T @event, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;
    Task PublishBatch<T>(IEnumerable<T> events, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;
    
    // 新增：延迟发布
    Task PublishDelayed<T>(T @event, TimeSpan delay, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;
    Task PublishDelayedBatch<T>(IEnumerable<(T Event, TimeSpan Delay)> events, string? routingKey = null, byte? priority = 0, CancellationToken cancellationToken = default) where T : IEvent;
}
```

**使用示例**：
```csharp
// 单条延迟消息
await _bus.PublishDelayed(new OrderTimeoutEvent 
{ 
    OrderId = "12345" 
}, TimeSpan.FromMinutes(30));

// 批量延迟消息（不同延迟时长）
var delayedEvents = new[]
{
    (new OrderTimeoutEvent { OrderId = "001" }, TimeSpan.FromMinutes(15)),
    (new OrderTimeoutEvent { OrderId = "002" }, TimeSpan.FromMinutes(30)),
    (new OrderTimeoutEvent { OrderId = "003" }, TimeSpan.FromHours(1))
};
await _bus.PublishDelayedBatch(delayedEvents);
```

---

## 四、边界情况处理

### 4.1 延迟时长边界

| 情况 | 处理策略 |
|------|---------|
| 零延迟或负延迟 | 抛出 `ArgumentException` |
| 超长延迟（>24小时） | 抛出异常，建议使用外部调度器 |
| 精确阈值边界（=10秒） | 使用分层队列，避免队头阻塞 |

### 4.2 消息顺序性

**重要**：延迟消息**不保证顺序**。

**解决方案**：
1. 在 Handler 中检查消息时间戳或版本号
2. 使用单消费者 + 顺序执行
3. 业务层实现幂等性

### 4.3 精确队列的队头阻塞

**缓解措施**：
1. 降低 `PreciseDelayThreshold`（如从 10 秒降至 5 秒）
2. 完全禁用精确队列：`options.PreciseDelayThreshold = TimeSpan.Zero`
3. 监控精确队列的消息堆积情况

---

## 五、性能考量

### 5.1 吞吐量分析

| 方案 | 延迟精度 | 吞吐量 | 内存占用 |
|------|---------|--------|---------|
| 纯精确队列 | 毫秒级 | 低（队头阻塞） | 低 |
| 纯分层队列 | 秒级-分钟级 | 高 | 中 |
| 混合方案（推荐） | 短延迟精确，长延迟分层 | 中-高 | 中 |

### 5.2 性能优化建议

1. **批量发布**：使用 `PublishDelayedBatch` 减少网络往返
2. **禁用发布确认**（非关键场景）：提升 2-3 倍吞吐量
3. **使用 SSD**：延迟队列写入密集
4. **惰性队列**：`x-queue-mode: lazy`，减少内存占用

### 5.3 内存占用估算

```
单条消息内存 ≈ 消息体大小 + 2KB（元数据 + RabbitMQ 开销）
100 万条 1KB 消息 → 约 3GB 内存
```

---

## 六、监控与可观测性

### 6.1 新增指标

```csharp
// 延迟消息指标
rabbitmq.publish.delayed.total          // 延迟消息发布总数
rabbitmq.delay.duration.seconds         // 延迟时长分布
rabbitmq.delay.queue.depth              // 延迟队列深度
rabbitmq.delay.accuracy.seconds         // 延迟精度（实际 vs 预期）
rabbitmq.delay.queue.overflow.total     // 队列溢出次数
```

### 6.2 告警规则

| 告警 | 条件 | 严重级别 |
|------|------|---------|
| 延迟队列深度过高 | depth > 50000 | Warning |
| 延迟队列深度严重过高 | depth > 100000 | Critical |
| 延迟队列溢出 | overflow_rate > 0 | Critical |
| 延迟精度下降 | P95 误差 > 10s | Warning |

---

## 七、总结与建议

### 7.1 方案对比

| 维度 | 方案 A | 方案 B | 方案 C（推荐） |
|------|--------|--------|---------------|
| 延迟精度 | 毫秒级 | 秒级-分钟级 | 兼顾 |
| 队头阻塞 | ❌ 严重 | ✅ 无 | ⚠️ 有限 |
| 吞吐量 | ❌ 低 | ✅ 高 | ✅ 中-高 |
| 生产可用性 | ❌ | ✅ | ✅✅ |

### 7.2 最佳实践

**DO（推荐）**：
- ✅ 使用 `PublishDelayedBatch` 批量发布
- ✅ 为延迟队列配置 `x-max-length`
- ✅ 使用仲裁队列提高可靠性
- ✅ 监控延迟精度指标
- ✅ 超长延迟使用外部调度器（Hangfire/Quartz）

**DON'T（避免）**：
- ❌ 在精确队列中混合长短延迟
- ❌ 依赖毫秒级精度
- ❌ 在延迟消息中传输大文件

### 7.3 实施路线图

| 阶段 | 内容 | 时间 |
|------|------|------|
| Phase 1 | 核心功能（PublishDelayed、拓扑初始化） | 2-3 周 |
| Phase 2 | 批量发布、监控指标、健康检查 | 1-2 周 |
| Phase 3 | 高级特性（长延迟存储、精度监控） | 1-2 周 |
| Phase 4 | 文档与示例 | 1 周 |

---

## 附录：完整配置示例

```csharp
builder.Services.AddRabbitBus(c =>
{
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!));
    
    c.WithDelayedMessaging(options =>
    {
        // 使用自定义延迟交换机名称，基于普通交换机 + DLX/TTL 拓扑实现延迟消息，而非依赖 rabbitmq-delayed-message-exchange 插件
        options.DelayExchangeName = "easilynet.delay";
        options.PreciseDelayThreshold = TimeSpan.FromSeconds(5);
        options.MaxDelay = TimeSpan.FromHours(24);
        options.AutoDeclareDelayQueues = true;
        options.DelayTiers = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(30),
            TimeSpan.FromHours(1),
            TimeSpan.FromHours(6),
            TimeSpan.FromHours(24)
        };
    });
    
    c.WithResilience(
        retryCount: 5,
        publisherConfirms: true,
        maxOutstandingConfirms: 1000);
    
    c.AddEvent<OrderTimeoutEvent>(EModel.Routing, "order.exchange", "order.timeout", "order.timeout.queue")
     .WithEventQueueArgs(new Dictionary<string, object?>
     {
         ["x-queue-type"] = "quorum",
         ["x-max-length"] = 100000
     })
     .WithHandler<OrderTimeoutHandler>();
});
```

---

> **参考资料**：
> - [RabbitMQ Dead Letter Exchanges](https://www.rabbitmq.com/docs/dlx)
> - [RabbitMQ TTL](https://www.rabbitmq.com/docs/ttl)
> - [rabbitmq-delayed-message-exchange (已停止维护)](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)
