# ManagedWebSocketClient — 托管 WebSocket 客户端

`ManagedWebSocketClient` 是对原生 `ClientWebSocket` 的高级封装。原生 `ClientWebSocket` 只负责"收发字节"，断线重连、保活、并发发送、缓冲区管理这些"生产环境必备但又繁琐易错"的工作都得你自己写。`ManagedWebSocketClient` 把这些全部内置，你只需要：**配置 → 订阅事件 → 连接 → 收发**。

> 命名空间：`EasilyNET.Core.WebSocket`
> 程序集：`EasilyNET.Core`

> **保活说明**：本库的连接保活/死链检测完全采用 **WebSocket 协议层的 Ping/Pong**（由 .NET 运行时处理），通过 `KeepAliveInterval` + `KeepAliveTimeout` 两个选项启用。**不再提供应用层"ping/pong 消息"心跳**——后者徒增复杂度、又容易和协议层心跳混淆。详见[保活与死链检测](#保活与死链检测)。

---

## 目录

- [60 秒快速上手](#60-秒快速上手)
- [核心概念（建立心智模型）](#核心概念建立心智模型)
- [分步详解](#分步详解)
  - [1. 配置选项](#1-配置选项)
  - [2. 订阅事件（同步 vs 异步）](#2-订阅事件同步-vs-异步)
  - [3. 连接 / 发送 / 断开 / 释放](#3-连接--发送--断开--释放)
- [保活与死链检测](#保活与死链检测)
- [完整配置表](#完整配置表)
- [最佳实践](#最佳实践)
- [常见问题 FAQ](#常见问题-faq)
- [释放与线程安全语义](#释放与线程安全语义)

---

## 60 秒快速上手

```csharp
using System.Text;
using EasilyNET.Core.WebSocket;

// 1) 配置：最少只需要一个地址（推荐再加上协议层保活，见下文）
var options = new WebSocketClientOptions
{
    ServerUri = new Uri("ws://localhost:5000/ws"),
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    KeepAliveTimeout = TimeSpan.FromSeconds(10)
};

// 2) 创建客户端（IAsyncDisposable，建议 await using 自动释放）
await using var client = new ManagedWebSocketClient(options);

// 3) 订阅"收到消息"事件
client.MessageReceived += (sender, e) =>
{
    var text = Encoding.UTF8.GetString(e.Data.Span);
    Console.WriteLine($"收到: {text}");
};

// 4) 连接并发送
await client.ConnectAsync();
await client.SendTextAsync("Hello WebSocket");

// 5) 用完断开（或直接交给 await using 释放）
await client.DisconnectAsync();
```

默认配置已经是合理的生产参数：**自动重连开启**、指数退避、4MB 单条消息上限。

---

## 核心概念（建立心智模型）

理解下面 4 点，就能避免 90% 的使用误区。

### ① 状态机

客户端在以下状态间流转，可通过 `client.State` 读取，或订阅 `StateChanged` 事件感知变化：

```
Disconnected ──ConnectAsync()──▶ Connecting ──成功──▶ Connected
     ▲                                                   │
     │                                            连接断开 │
     │                                                   ▼
     └────── 重试耗尽/手动断开 ◀── Reconnecting ◀──── (AutoReconnect=true)
```

| 状态             | 含义                          |
|----------------|-----------------------------|
| `Disconnected` | 未连接，也未在尝试连接                 |
| `Connecting`   | 正在首次建立连接                    |
| `Connected`    | 已连接，可收发消息                   |
| `Reconnecting` | 连接断开后正在自动重连                 |
| `Closing`      | 正在优雅关闭                      |
| `Disposed`     | 已释放，**终止态**，不可再用            |

> **只有 `Connected` 状态才能发送消息**，否则 `SendAsync` 系列会抛 `InvalidOperationException`。

### ② 发送是"入队 + 后台单线程发送"

你调用 `SendTextAsync` / `SendBinaryAsync` 时，消息先进入一个有界队列（`Channel`），由唯一的后台发送循环按**严格顺序**发到 socket。这带来三个好处：

- **并发安全**：多个线程同时发送不会交错损坏帧（WebSocket 不允许并发写同一连接）。
- **顺序保证**：消息按入队顺序发送。
- **背压**：队列满（`SendQueueCapacity`）时，`SendAsync` 会异步等待，而不是无限堆积内存。

`WaitForSendCompletion` 决定 `await SendAsync` 何时返回：
- `true`（默认）：等到**真正写入 socket 完成**，发送异常会从 `await` 抛出 → 适合需要确认送达的场景。
- `false`：**入队成功即返回**（吞吐更高），发送失败只通过 `Error` 事件报告，断线时排队中的消息可能被丢弃。

### ③ 接收与回调是"解耦"的

接收循环只负责从 socket 读取并组装完整消息，然后**入队**给一个专门的分发循环去调用你的事件处理器。这意味着：

- 你的**消息处理器再慢，也不会卡住接收循环**。
- 消息按到达顺序投递（单分发循环）。
- 分发队列满（`ReceiveDispatchQueueCapacity`）时会对接收侧产生背压，限制内存。

### ④ 接收缓冲区是"借来的"，回调返回后立即归还

`MessageReceived` / `MessageReceivedAsync` 事件参数里的 `e.Data` 背后是 `ArrayPool` 租用的缓冲区，**仅在回调执行期间有效**。回调一返回，缓冲区就被归还复用。

- ✅ 在回调内**立即**读取/拷贝：`Encoding.UTF8.GetString(e.Data.Span)`、`e.Data.ToArray()`。
- ❌ 把 `e.Data` 存进字段/列表/闭包"留着以后用" —— 之后再访问会读到被复用的脏数据。
- 需要异步处理（`await`）时，请用 `MessageReceivedAsync`（它会等你的异步处理器完成后才归还缓冲区），或先 `ToArray()` 拷贝出来。

---

## 分步详解

### 1. 配置选项

`WebSocketClientOptions` 的属性都是 `init` 的，创建后不可变。只有 `ServerUri` 是必填，其余都有合理默认值。

```csharp
var options = new WebSocketClientOptions
{
    ServerUri = new Uri("wss://example.com/ws"),

    // ——— 保活 / 死链检测（协议层，推荐开启）———
    KeepAliveInterval = TimeSpan.FromSeconds(15),   // 每 15s 发一次协议层 PING
    KeepAliveTimeout = TimeSpan.FromSeconds(10),    // 10s 内无 PONG 则运行时中止连接 → 触发重连

    // ——— 重连 ———
    AutoReconnect = true,                          // 断线自动重连（默认 true）
    MaxReconnectAttempts = 5,                      // 最多重连 5 次，-1 表示无限重试
    ReconnectDelay = TimeSpan.FromSeconds(1),      // 初始重连延迟
    MaxReconnectDelay = TimeSpan.FromSeconds(30),  // 退避上限
    UseExponentialBackoff = true,                  // 指数退避 + ±20% 抖动，避免雷群

    // ——— 收发 ———
    ReceiveBufferSize = 16384,                     // 单次读取缓冲区（16KB）
    MaxMessageSize = 4 * 1024 * 1024,              // 单条消息上限（4MB），超限断连防内存耗尽
    SendQueueCapacity = 1000,                      // 发送队列容量
    ReceiveDispatchQueueCapacity = 1024,           // 接收分发队列容量
    WaitForSendCompletion = true,                  // SendAsync 是否等真正发送完成

    // ——— 超时 ———
    ConnectionTimeout = TimeSpan.FromSeconds(10),

    // ——— 原生 ClientWebSocket 配置（Header / Proxy / 证书 / 子协议）———
    RequestedSubProtocols = ["chat", "v1"],
    ConfigureWebSocket = socket =>
    {
        socket.Options.SetRequestHeader("Authorization", "Bearer <token>");
    }
};
```

### 2. 订阅事件（同步 vs 异步）

| 事件                    | 签名                                                    | 何时用                                          |
|-----------------------|-------------------------------------------------------|----------------------------------------------|
| `MessageReceived`     | `EventHandler<WebSocketMessageReceivedEventArgs>`     | **同步**处理消息（解析、入队到你自己的队列）。回调内不要 `await` 长任务   |
| `MessageReceivedAsync`| `Func<self, args, ValueTask>`                         | 需要 `await` 的**异步**处理。缓冲区会等你完成后再归还             |
| `StateChanged`        | `EventHandler<WebSocketStateChangedEventArgs>`        | 感知连接状态变化（更新 UI、日志）                           |
| `Reconnecting`        | `EventHandler<WebSocketReconnectingEventArgs>`        | 每次重连尝试前触发，可设 `e.Cancel = true` 取消后续重连        |
| `Error`               | `EventHandler<WebSocketErrorEventArgs>`               | 观察后台错误（发送失败、连接中止、重连失败等），`e.Context` 标明出错位置    |
| `Closed`              | `EventHandler<WebSocketClosedEventArgs>`              | 连接关闭。`e.InitiatedByClient` 区分是本端主动还是对端/异常导致   |

```csharp
// 同步：快速处理
client.MessageReceived += (s, e) =>
{
    var text = Encoding.UTF8.GetString(e.Data.Span);   // 回调内立即读取
    Console.WriteLine($"收到: {text}");
};

// 异步：可以 await（缓冲区安全有效，直到你处理完）
client.MessageReceivedAsync += async (s, e) =>
{
    var text = Encoding.UTF8.GetString(e.Data.Span);
    await SaveToDatabaseAsync(text);
};

client.StateChanged  += (s, e) => Console.WriteLine($"{e.PreviousState} → {e.CurrentState}");
client.Reconnecting  += (s, e) => Console.WriteLine($"第 {e.AttemptNumber} 次重连，延迟 {e.Delay.TotalSeconds}s");
client.Error         += (s, e) => Console.WriteLine($"[{e.Context}] {e.Exception.Message}");
client.Closed        += (s, e) => Console.WriteLine($"关闭: {e.CloseDescription}（本端发起: {e.InitiatedByClient}）");
```

> 事件处理器内部抛出的异常会被框架捕获并写入 `Debug` 输出，**不会**导致客户端崩溃，但也意味着你的异常被"吞掉"了——重要逻辑请自行 try/catch。

### 3. 连接 / 发送 / 断开 / 释放

```csharp
// 连接（可传 CancellationToken）
await client.ConnectAsync();

// 发送文本
await client.SendTextAsync("hello");

// 发送二进制
byte[] payload = [0x01, 0x02, 0x03];
await client.SendBinaryAsync(payload);
await client.SendBinaryAsync(payload.AsMemory());          // ReadOnlyMemory 重载
await client.SendAsync(payload, WebSocketMessageType.Binary); // 完全自定义

// 主动断开（会阻止自动重连，直到下次 ConnectAsync）
await client.DisconnectAsync();

// 释放（IAsyncDisposable）。await using 会在作用域结束时自动调用
await client.DisposeAsync();
```

> `DisconnectAsync` 与 `DisposeAsync` 的区别：前者只是断开，对象仍可再次 `ConnectAsync`；后者彻底释放，进入 `Disposed` 终止态，不可再用。

---

## 保活与死链检测

WebSocket 协议（RFC 6455）自带 **Ping/Pong 控制帧**，这是标准的连接保活机制。.NET 运行时能自动收发这些帧——你**无法**手动发送或观察它们（`ReceiveAsync` 永远只返回 Text/Binary/Close），但可以用两个选项配置运行时的自动行为：

| 选项 | 作用 |
|---|---|
| `KeepAliveInterval` | 运行时按此间隔自动发送协议层 PING |
| `KeepAliveTimeout`（.NET 8+） | 发送 PING 后超时仍无 PONG → 运行时 **abort** 连接 |

**两者一起设置**才能得到真正的死链检测：

```csharp
var options = new WebSocketClientOptions
{
    ServerUri = new Uri("wss://example.com/ws"),
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    KeepAliveTimeout = TimeSpan.FromSeconds(10)
};
```

连接死掉时，运行时因 PONG 超时而 abort → 接收循环抛异常 → 触发库的**自动重连**。整个过程在协议层完成，零应用消息、零业务代码。

> **只设 `KeepAliveInterval`、不设 `KeepAliveTimeout`**：运行时只发送 PONG 帧维持中间设备（NAT/代理）存活，**不会检测对端死活**。要死链检测，两个都要设。

> **为什么不做应用层心跳？** 应用层"ping/pong 消息"需要两端写收发逻辑、容易和协议层心跳混淆、还会把心跳混进业务消息流。协议层 Ping/Pong 是 RFC 强制行为，任何合规的服务端（包括本库的服务端 `EasilyNET.WebCore`）都会自动回应，因此协议层方案更简洁可靠。本库已移除应用层心跳。

> ⚠️ **两个边界情况**：① 协议层 Pong 由对端运行时自动回复，只证明"传输 + 运行时存活"，不保证对端业务循环没卡死；② 极少数"哑"代理只把数据帧计为活动、不认控制帧。若你的部署确实命中这两种情况，可在**业务消息里**自行附带轻量探活（这是普通业务逻辑，不属于本库职责）。

---

## 完整配置表

| 属性                              | 类型                            | 默认值      | 说明                                                                    |
|---------------------------------|-------------------------------|----------|-----------------------------------------------------------------------|
| `ServerUri`                     | `Uri?`                        | `null`   | **必填**。WebSocket 服务器地址（`ws://` 或 `wss://`）                            |
| `KeepAliveInterval`             | `TimeSpan?`                   | `null`   | **协议层** PING 间隔（运行时处理）                                                |
| `KeepAliveTimeout`              | `TimeSpan?`                   | `null`   | **协议层** PING/PONG 超时（.NET 8+）。与 `KeepAliveInterval` 同设即开启死链检测       |
| `AutoReconnect`                 | `bool`                        | `true`   | 是否启用自动重连                                                              |
| `MaxReconnectAttempts`          | `int`                         | `5`      | 最大重连次数，`-1` 表示无限                                                      |
| `ReconnectDelay`                | `TimeSpan`                    | 1 秒      | 初始重连延迟                                                                |
| `MaxReconnectDelay`             | `TimeSpan`                    | 30 秒     | 重连延迟上限（指数退避封顶）                                                        |
| `UseExponentialBackoff`         | `bool`                        | `true`   | 指数退避；额外叠加 ±20% 抖动防雷群                                                  |
| `ConnectionTimeout`             | `TimeSpan`                    | 10 秒     | 单次连接超时                                                                |
| `ReceiveBufferSize`             | `int`                         | 16384    | 单次接收缓冲区大小（字节）                                                         |
| `MaxMessageSize`                | `long`                        | 4MB      | 单条消息最大字节数，超限则断开连接防内存耗尽；设为 0 或负数禁用（不推荐）                                |
| `SendQueueCapacity`             | `int`                         | 1000     | 发送队列容量（满时 `SendAsync` 背压等待）                                           |
| `ReceiveDispatchQueueCapacity`  | `int`                         | 1024     | 接收分发队列容量（满时对接收侧背压）                                                    |
| `WaitForSendCompletion`         | `bool`                        | `true`   | `true`=等真正发送完成并抛出发送异常；`false`=入队即返回，错误经 `Error` 事件报告，断线时排队消息可能丢失     |
| `RequestedSubProtocols`         | `IReadOnlyList<string>?`      | `null`   | 请求的子协议列表（自动去重/去空）                                                     |
| `ConfigureWebSocket`            | `Action<ClientWebSocket>?`    | `null`   | 直接配置底层 socket（Header、Proxy、证书等）                                       |
| `DisposeLockTimeout`            | `TimeSpan`                    | 5 秒      | `DisposeAsync` 首次等待内部连接锁的超时                                           |
| `DisposeLockTimeoutGracePeriod` | `TimeSpan`                    | 25 秒     | 首次等待超时后的额外宽限；超总预算后退化为 best-effort 清理                                  |

---

## 最佳实践

- **生命周期**：把 `ManagedWebSocketClient` 当作长生命周期对象（如单例/服务字段），不要每次发消息都新建。用 `await using` 或在宿主停止时 `DisposeAsync`。
- **开启协议层保活**：生产环境建议同时设置 `KeepAliveInterval` + `KeepAliveTimeout`，让运行时自动检测死链并触发重连。
- **回调要快**：`MessageReceived` 里只做轻量解析；重活交给 `MessageReceivedAsync`，或拷贝数据后丢到你自己的处理管线。
- **务必订阅 `Error`**：当 `WaitForSendCompletion = false` 时，发送失败**只**通过 `Error` 事件暴露。
- **鉴权**：用 `ConfigureWebSocket` 设置 `Authorization` 头或在 `ServerUri` 上带 query token。
- **发送前判状态**：高并发下可先判断 `client.State == WebSocketClientState.Connected`，或捕获 `InvalidOperationException`。

---

## 常见问题 FAQ

**Q：发送时报 `InvalidOperationException: Client is not connected`？**
A：只有 `Connected` 状态能发送。请确保 `ConnectAsync` 已完成，且当时未处于重连中。

**Q：`e.Data` 在另一个线程/稍后访问变成乱码？**
A：缓冲区回调返回即归还。请在回调内 `ToArray()` 拷贝，或改用 `MessageReceivedAsync`。

**Q：怎么检测连接掉线并自动重连？**
A：设置 `KeepAliveInterval` + `KeepAliveTimeout` 开启协议层死链检测；运行时 abort 后库会自动重连（受 `AutoReconnect` / `MaxReconnectAttempts` 控制）。

**Q：只设了 `KeepAliveInterval` 却检测不到掉线？**
A：必须同时设置 `KeepAliveTimeout`，否则运行时只维持中间设备存活、不做超时中止。

**Q：怎么发超过 4MB 的消息？**
A：调大 `MaxMessageSize`。注意服务端也要有相应上限。

**Q：`MessageReceivedAsync` 里的异常去哪了？**
A：会被捕获并通过 `Error` 事件上报（Context 为 `MessageReceivedAsync handler`），不会中断接收。

---

## 释放与线程安全语义

- 所有公共方法都是**线程安全**的（发送需在 `Connected` 状态）。
- `StateChanged` 与 `ConfigureWebSocket` 都在**不持有内部锁**时回调，因此你可以在其中安全地同步调用 `ConnectAsync()` / `DisconnectAsync()` 而不会死锁。
- `Disposed` 是**终止态**：一旦释放，任何状态回退都不会再把它改回其他状态。
- `DisposeAsync` 的清理顺序：
  1. 先等待 `DisposeLockTimeout` 获取内部连接锁；
  2. 拿不到则再等 `DisposeLockTimeoutGracePeriod`；
  3. 总预算内仍拿不到（例如用户回调阻塞在锁内），则执行 **best-effort 清理**：标记已释放、取消后台循环、失败剩余发送，并**跳过**可能与并发操作冲突的 socket/CTS/锁释放，避免永久挂起或竞争。
