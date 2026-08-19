# RabbitMQ 延迟消息实现方案：二进制延迟阶梯（Binary Delay Ladder）

> 本文档描述 `EasilyNET.RabbitBus` 当前的延迟投递设计。实现完全基于原生 AMQP 能力（普通 topic 交换机 + 队列级 TTL +
> 死信转发），**不依赖任何 broker 插件**，可运行在仲裁队列与集群环境上。
>
> 设计参考 [NServiceBus 的 RabbitMQ 传输层](https://github.com/Particular/NServiceBus)（`DelayInfrastructure`
> 的分层延迟拓扑），并针对本库「以交换机为中心」的事件模型做了扩展。

---

## 一、为什么不再使用 `rabbitmq-delayed-message-exchange`

RabbitMQ 官方已于 2026-01-29 宣布停止维护该插件：

1. **运行基础是 Mnesia**：延迟消息只存在于单个节点，无法在集群中复制，节点故障即丢消息；
2. **Mnesia 已被弃用**：RabbitMQ 在 4.3 开发周期中弃用 Mnesia，插件将无法继续工作；
3. **不适合大规模场景**：设计上难以承载数十万乃至数百万条延迟消息；
4. **重构代价过高**：分布式化需要从「自定义交换机类型」改为「自定义队列类型」，成本以人年计。

因此本库改用官方推荐路径（DLX + TTL）的一种**工程化形态**：二进制延迟阶梯。

---

## 二、朴素 DLX + TTL 方案的问题

直接用「一个延迟队列 + 消息级 TTL」会踩到 RabbitMQ 的经典陷阱：

```
[延迟交换机] → [单个延迟队列] --TTL过期--> [DLX] → [目标队列]
```

**队头阻塞**：RabbitMQ 只检查队列**头部**消息是否过期。先入队的 1 小时延迟消息会挡住后入队的 1 秒延迟消息，
导致后者迟到近 1 小时。这使得单队列 + 消息级 TTL 在生产环境不可用。

「按档位切分队列（1s / 5s / 30s / 1m ...）」可以规避队头阻塞——每个队列内的 TTL 完全一致——
但代价是**延迟精度损失**：请求 47 秒只能落到 1 分钟档位。

二进制延迟阶梯同时解决这两个问题：**队列内 TTL 完全一致（无队头阻塞）**，且**延迟精确到秒（无精度损失）**。

---

## 三、核心机制：把延迟秒数编码成路由键

设阶梯有 `N` 个档位（`level 0 .. level N-1`），档位 `n` 的队列 TTL 固定为 `2^n` 秒。

延迟秒数按二进制展开写进路由键：

```
routingKey = b(N-1) . b(N-2) . ... . b1 . b0 . <延迟地址>
```

消息从「最高有效位对应的档位交换机」进入阶梯，随后逐级下降：

| 当前档位 `n` 的位 | 行为                                                          | 耗时      |
|-------------|-------------------------------------------------------------|---------|
| `1`         | 进入档位 `n` 的队列等待，TTL 到期后死信到档位 `n-1` 的交换机                       | `2^n` 秒 |
| `0`         | 交换机到交换机绑定直接透传到档位 `n-1`，不落任何队列                               | 0       |

走到档位 0 之后进入**投递交换机**，由它把消息交给最终目标。所有停留时间之和 `Σ 2^n（位为 1）`
恰好等于请求的延迟秒数。

### 3.1 拓扑示意（以 4 档为例）

```
                     ┌── 1.#      ──> [level-03 queue] (ttl=8s) ──DLX──┐
publish ──> [level-03]                                                 │
                     └── 0.#      ─────────────────────────────────────┤
                                                                       ▼
                     ┌── *.1.#    ──> [level-02 queue] (ttl=4s) ──DLX──┐
                    [level-02]                                         │
                     └── *.0.#    ─────────────────────────────────────┤
                                                                       ▼
                     ┌── *.*.1.#  ──> [level-01 queue] (ttl=2s) ──DLX──┐
                    [level-01]                                         │
                     └── *.*.0.#  ─────────────────────────────────────┤
                                                                       ▼
                     ┌── *.*.*.1.# ─> [level-00 queue] (ttl=1s) ──DLX──┐
                    [level-00]                                         │
                     └── *.*.*.0.# ────────────────────────────────────┤
                                                                       ▼
                                                                  [delivery]
                                                                       │  #.<延迟地址>
                                                                       ▼
                                                            目标队列 / 目标交换机
```

**举例**：延迟 5 秒 = `0b0101`，地址 `e.order_exchange.order.timeout`

```
routingKey = 0.1.0.1.e.order_exchange.order.timeout

level-03(位0) 透传 → level-02(位1) 等 4s → level-01(位0) 透传 → level-00(位1) 等 1s → delivery → 目标
合计 4 + 1 = 5 秒 ✅
```

### 3.2 关键属性

| 属性          | 说明                                                        |
|-------------|-----------------------------------------------------------|
| **无队头阻塞**   | 每个档位队列内所有消息的 TTL 完全相同，先进先出天然等价于先到期先出队                     |
| **秒级精确**    | 任意秒数都能被二进制唯一表达，无「向上取整到档位」的精度损失                            |
| **跳数有界**    | 最多 `N` 跳（默认 28），与延迟长短无关                                    |
| **存储可控**    | 位为 0 的档位不落队列，一条延迟消息同一时刻只存在于一个队列中                          |
| **集群友好**    | 阶梯队列默认声明为**仲裁队列**（`x-queue-type=quorum`），可复制、可容忍节点故障      |
| **无插件依赖**   | 全部使用原生 topic 交换机 / 队列 TTL / DLX，任何 RabbitMQ 版本可用          |

### 3.3 容量

| 档位数        | 最大延迟             | 需声明的交换机 + 队列 |
|------------|------------------|-------------|
| 8          | 255 秒（约 4 分钟）    | 8 + 8       |
| 12         | 4095 秒（约 68 分钟）  | 12 + 12     |
| 17         | 131071 秒（约 36 小时） | 17 + 17     |
| 21         | 约 24 天           | 21 + 21     |
| **28（默认）** | **约 8.5 年**      | 28 + 28     |

> 档位数同时决定路由键的宽度，**同一套阶梯的生产端与消费端必须使用相同的档位数**。
> 修改档位数等于换了一套拓扑，建议同时修改 `Prefix` 以免与旧拓扑混用。

---

## 四、延迟地址：让延迟发布与普通发布抵达同一批消费者

NServiceBus 的延迟只用于「点对点发送」，路由键末尾直接是目标队列名。本库的事件模型是**以交换机为中心**的，
因此把末尾的这一段抽象为**延迟地址**，并按事件的交换机语义推导（`AddressMode = RoutingAware`，默认）：

| `EModel`                | 延迟地址              | 绑定对象                          | 效果                                     |
|-------------------------|-------------------|-------------------------------|----------------------------------------|
| `Routing`（direct）       | `e.{交换机}.{路由键}`   | 队列 ← `#.e.{交换机}.{绑定键}`        | 精确匹配，等价于 direct 语义                     |
| `Topics`（topic）         | `e.{交换机}.{路由键}`   | 队列 ← `#.e.{交换机}.{绑定模式}`       | 投递交换机本身是 topic，`*` / `#` 通配符依然生效       |
| `PublishSubscribe`（fanout） | `x.{交换机}`      | **交换机** ← `#.x.{交换机}`         | 交换机到交换机绑定，消息重回目标 fanout 交换机，正常扇出到所有队列  |
| `None`（默认交换机）           | `q.{队列}`          | 队列                            | 点对点直投                                  |
| `Headers`               | `q.{队列}`          | 队列                            | topic 无法复刻头部匹配，退化为直投所配置的队列             |

**为什么发布端与消费端算出来的地址能对上**：发布端用「本次发布的实际路由键」，
消费端用「队列自身的绑定模式」。二者在 topic 匹配规则下天然对齐——
消费端绑定 `#.e.topic_exchange.topic.queue.*`，发布端 `topic.queue.1` 的消息就会命中，
与普通发布经过 topic 交换机的结果完全一致。

另一种模式 `AddressMode = QueueDirect` 则始终使用 `q.{队列}`，与 NServiceBus 行为一致：
拓扑最简单，但 fanout 事件只会抵达自身队列。也可以用 `.WithDelayAddress("...")` 为单个事件显式指定地址。

> **绑定由消费端建立**。这与普通交换机绑定的职责划分一致：生产端只声明阶梯，消费端在声明队列时
> 顺带把自己绑定到投递交换机。因此**消费端进程也必须启用延迟投递**，否则消息到期后无处可去会被静默丢弃。

---

## 五、配置与 API

### 5.1 启用

```csharp
builder.Services.AddRabbitBus(c =>
{
    c.WithConnection(f => f.Uri = new(builder.Configuration.GetConnectionString("Rabbit")!))
     // 24 小时上限 → 自动换算为 17 个档位；单节点开发环境可关掉仲裁队列
     .WithDelayedDelivery(TimeSpan.FromHours(24), useQuorumQueues: false);

    c.AddEvent<OrderTimeoutEvent>(EModel.Routing, "order_exchange", "order.timeout", "order_timeout_queue")
     .WithHandler<OrderTimeoutHandler>();
});
```

需要精细控制时使用委托重载：

```csharp
c.WithDelayedDelivery(o =>
{
    o.Prefix = "myapp.v1.delay";               // 拓扑名称前缀
    o.LevelCount = 21;                          // 直接指定档位数（约 24 天）
    o.AddressMode = EDelayAddressMode.RoutingAware;
    o.UseQuorumQueues = true;
    o.AutoDeclareTopology = true;               // 拓扑由 IaC 预置时设为 false
    o.QueueArguments["x-max-length"] = 1_000_000;
});
```

### 5.2 发布

```csharp
// 相对延迟
await bus.PublishDelayed(new OrderTimeoutEvent { OrderId = "12345" }, TimeSpan.FromMinutes(30));

// 绝对时间（DoNotDeliverBefore 语义）
await bus.PublishAt(new OrderTimeoutEvent { OrderId = "12345" }, DateTimeOffset.Now.AddHours(2));

// 批量（共享同一延迟）
await bus.PublishDelayedBatch(events, TimeSpan.FromMinutes(15));

// 非泛型
await bus.PublishDelayed(evt, typeof(OrderTimeoutEvent), TimeSpan.FromSeconds(30));
```

事件本身、处理器、中间件、回退处理器的写法与普通事件**完全一致**——延迟只体现在发布端调用的 API 上。

### 5.3 边界行为

| 情况                       | 行为                                          |
|--------------------------|---------------------------------------------|
| 延迟 ≤ 0 / `PublishAt` 时间已过 | 等同于普通发布，不进入阶梯                               |
| 亚秒级延迟                    | 向上取整到 1 秒（保证不提前投递）                          |
| 超过阶梯上限                   | 抛出 `ArgumentOutOfRangeException`，提示提高档位数或改用外部调度器 |
| 未启用延迟投递却调用延迟 API         | 抛出 `InvalidOperationException`               |
| 发布确认 nack / 超时           | 按**剩余时间**重投（保持原定投递时刻），而不是从头再延迟一次            |

---

## 六、可观测性

### 6.1 消息头（仅用于诊断，路由完全依赖路由键）

| Header                       | 含义                       |
|------------------------------|--------------------------|
| `x-easilynet-delay-seconds`  | 请求的延迟秒数                  |
| `x-easilynet-deliver-at`     | 期望投递时间（UTC，`O` 格式）       |
| `x-easilynet-delay-address`  | 本条消息的延迟地址                |

### 6.2 指标（Meter：`EasilyNET.RabbitBus`）

| 指标                                      | 类型        | 含义                                |
|-----------------------------------------|-----------|-----------------------------------|
| `rabbitmq.publish.delayed.total`        | Counter   | 进入阶梯的延迟消息总数                       |
| `rabbitmq.delay.requested.seconds`      | Histogram | 请求延迟时长分布                          |
| `rabbitmq.delay.delivery.error.seconds` | Histogram | **实际到达时间 − 期望投递时间**，即端到端延迟精度      |

### 6.3 链路追踪（ActivitySource：`EasilyNET.RabbitBus`）

发布侧 `rabbitmq.publish` 附加 `messaging.rabbitmq.delay_seconds` / `delay_address` / `delay_level_exchange`；
消费侧 `rabbitmq.consume` 附加 `messaging.rabbitmq.delay_error_seconds`。

---

## 七、运维要点

1. **档位数是拓扑契约**：生产端与消费端不一致会导致路由键宽度不匹配，消息无法命中任何绑定。
2. **消费端必须启用延迟投递**：绑定由消费端建立；只启用生产端会让消息在投递交换机处被丢弃。
3. **拓扑是幂等声明**：启动时与每次重连后都会重新声明；使用独立通道，声明失败不会拖垮发布通道。
4. **不保证顺序**：不同延迟的消息到期顺序与发布顺序无关，处理器需保证幂等。
5. **仲裁队列参数**：`x-dead-letter-strategy=at-least-once` 必须配合 `x-overflow=reject-publish`，本库已自动设置。
6. **观察堆积**：阶梯队列名为 `{Prefix}-level-NN`，可在管理界面直接看到每个档位的堆积量。
7. **超长延迟**：超过数月的定时任务更适合交给外部调度器（Hangfire / Quartz）+ 持久化存储，
   到点后再发普通消息；阶梯适合「分钟到数天」这一主流区间。
8. **direct 路由键中的 `*` / `#`**：投递交换机是 topic 类型，这两个字符会被当作通配符解释，
   启动时会记录告警日志。

---

## 八、与旧插件方案的对照

| 维度      | `rabbitmq-delayed-message-exchange` | 二进制延迟阶梯                    |
|---------|-------------------------------------|----------------------------|
| 依赖      | 需安装插件                               | 无                          |
| 存储      | Mnesia（单节点、已弃用）                     | 普通队列，可用仲裁队列复制              |
| 集群安全    | ❌ 节点故障丢消息                           | ✅                          |
| 规模      | ❌ 不适合海量延迟消息                         | ✅ 与普通队列同级                  |
| 精度      | 毫秒                                  | 秒                          |
| 拓扑成本    | 1 个交换机                              | `N` 个交换机 + `N` 个队列（默认 28）  |
| 队头阻塞    | 无                                   | 无                          |
| 官方状态    | 停止维护                                | 基于官方推荐的 DLX + TTL          |

---

> **参考资料**
> - [NServiceBus RabbitMQ Transport - Delayed Delivery](https://github.com/Particular/NServiceBus)
> - [RabbitMQ Dead Letter Exchanges](https://www.rabbitmq.com/docs/dlx)
> - [RabbitMQ TTL](https://www.rabbitmq.com/docs/ttl)
> - [rabbitmq-delayed-message-exchange（已停止维护）](https://github.com/rabbitmq/rabbitmq-delayed-message-exchange)
