# WebSocket Server — ASP.NET Core 服务端

基于 ASP.NET Core 的高性能 WebSocket 服务端。你只需继承一个 `WebSocketHandler` 基类、重写几个事件方法，剩下的连接生命周期管理、并发发送、会话跟踪、广播、内存池化全部由框架处理。

> 命名空间：`EasilyNET.WebCore.WebSocket`
> 注册扩展位于 `Microsoft.Extensions.DependencyInjection` / `Microsoft.AspNetCore.Builder`（`using` 这两个即可）
> 程序集：`EasilyNET.WebCore`

> **保活说明**：连接保活/死链检测采用 **WebSocket 协议层 Ping/Pong**（由运行时处理），通过 `KeepAliveInterval` + `KeepAliveTimeout` 启用。**不提供应用层"ping/pong 消息"心跳**——避免无谓的复杂度与混淆。详见[保活与死链检测](#保活与死链检测)。

---

## 目录

- [3 步快速上手](#3-步快速上手)
- [核心概念（建立心智模型）](#核心概念建立心智模型)
- [分步详解](#分步详解)
  - [1. 定义处理程序](#1-定义处理程序-websockethandler)
  - [2. 注册服务](#2-注册服务)
  - [3. 映射路由](#3-映射路由)
- [会话与广播](#会话与广播)
- [保活与死链检测](#保活与死链检测)
- [完整配置表](#完整配置表)
- [核心组件](#核心组件)
- [最佳实践](#最佳实践)
- [常见问题 FAQ](#常见问题-faq)

---

## 3 步快速上手

```csharp
// ① 定义处理程序：重写你关心的事件
public sealed class EchoHandler(ILogger<EchoHandler> logger) : WebSocketHandler
{
    public override Task OnConnectedAsync(IWebSocketSession session)
    {
        logger.LogInformation("已连接: {Id}", session.Id);
        return session.SendTextAsync("welcome");
    }

    // 唯一必须实现的方法
    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(message.Data.Span);
            await session.SendTextAsync($"echo: {text}");
        }
    }
}
```

```csharp
// ② 注册：Handler 必须进 DI 容器；SessionManager 可选（需要广播时加）
builder.Services.AddSingleton<EchoHandler>();
builder.Services.AddWebSocketSessionManager();   // 可选

// ③ 映射：先 UseWebSockets()，再把 Handler 映射到路径
var app = builder.Build();
app.UseWebSockets();
app.MapWebSocketHandler<EchoHandler>("/ws/echo");
app.Run();
```

客户端连 `ws://your-host/ws/echo` 即可。默认参数即生产可用：4MB 单条消息上限、自动会话清理。建议再配上协议层保活（见下文）。

---

## 核心概念（建立心智模型）

### ① 事件回调的顺序保证

每个连接的回调遵循严格顺序：

```
OnConnectedAsync  →  OnMessageAsync × N  →  OnDisconnectedAsync
        （任意阶段出错则额外触发 OnErrorAsync）
```

- **`OnConnectedAsync` 一定在任何 `OnMessageAsync` 之前完成**。所以你可以放心在 `OnConnectedAsync` 里做鉴权、初始化 `session.Items`，不用担心消息"插队"先到。
- `OnMessageAsync` 按消息到达顺序**串行**调用（单分发循环），同一连接内不会并发回调。
- 只有当 `OnConnectedAsync` 成功返回后，框架才认为"已连接"，断开时才会调用 `OnDisconnectedAsync`。

### ② 接收与回调解耦

接收循环只负责从 socket 读取、组装完整消息，然后入队给独立的分发循环调用 `OnMessageAsync`。因此你的 `OnMessageAsync` **再慢也不会阻塞接收循环**。分发队列满（`ReceiveDispatchQueueCapacity`）时对接收侧背压，限制内存。

### ③ 发送是"入队 + 后台单线程发送"

`session.SendAsync` / `SendTextAsync` / `SendBinaryAsync` 把消息放进有界队列，由唯一发送循环按顺序写出。多处并发调用同一 session 的发送是安全的（不会交错损坏帧）。发送为**即发即弃**：方法在入队成功后即返回，实际发送错误记入日志。

### ④ `message.Data` 的有效期

`OnMessageAsync` 的 `message.Data` 是该条消息的独立数组，在回调内可安全读取。需要长期保留时仍建议 `ToArray()` 拷贝。`Encoding.UTF8.GetString(message.Data.Span)` 是最常见的读法。

### ⑤ 自动防护：超大消息

单条消息累计超过 `MaxMessageSize`（默认 4MB）时，框架会以 `MessageTooBig` 关闭该连接并记录告警——防止恶意或异常客户端用超大消息/永不结束的分片耗尽服务端内存。

---

## 分步详解

### 1. 定义处理程序 (`WebSocketHandler`)

`WebSocketHandler` 有 4 个方法，只有 `OnMessageAsync` 是 `abstract` 必须实现，其余 `virtual` 可选重写：

| 方法                    | 必须 | 触发时机                  |
|-----------------------|----|-----------------------|
| `OnConnectedAsync`    | 否  | 连接建立、进入消息循环前          |
| `OnMessageAsync`      | **是** | 每收到一条完整消息             |
| `OnDisconnectedAsync` | 否  | 连接断开（仅当曾成功 `OnConnected`） |
| `OnErrorAsync`        | 否  | 接收/处理过程中发生异常          |

> Handler 实例由 DI 解析。它通常是**单例**，被所有连接共享——因此**不要在 Handler 字段里存单个连接的状态**，按连接的数据请放在 `session.Items` 里。

```csharp
public sealed class ChatHandler(
    IWebSocketSessionManager sessions,
    ILogger<ChatHandler> logger) : WebSocketHandler
{
    public override async Task OnConnectedAsync(IWebSocketSession session)
    {
        // 按连接的数据存在 session.Items（线程安全字典）
        session.Items["JoinedAt"] = DateTime.UtcNow;
        session.Items["Name"] = "匿名";

        await session.SendTextAsync("欢迎加入聊天室！");
        await sessions.BroadcastTextAsync($"用户 {session.Id} 加入了");
    }

    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        if (message.MessageType != WebSocketMessageType.Text) return;
        var text = Encoding.UTF8.GetString(message.Data.Span);
        var name = session.Items["Name"] as string ?? session.Id;
        await sessions.BroadcastTextAsync($"[{name}]: {text}");
    }

    public override async Task OnDisconnectedAsync(IWebSocketSession session)
        => await sessions.BroadcastTextAsync($"用户 {session.Id} 离开了");

    public override Task OnErrorAsync(IWebSocketSession session, Exception exception)
    {
        logger.LogWarning(exception, "会话 {Id} 出错", session.Id);
        return Task.CompletedTask;
    }
}
```

### 2. 注册服务

```csharp
// Handler 必须注册到 DI（MapWebSocketHandler 会校验，未注册会抛异常）
builder.Services.AddSingleton<ChatHandler>();

// 会话管理器：需要广播 / 遍历在线连接时注册（单例）
builder.Services.AddWebSocketSessionManager();
```

> 如果你用 EasilyNET 的 `AutoDependencyInjection`，也可以给 Handler 打 `[DependencyInjection(ServiceLifetime.Singleton)]` 特性自动注册。

### 3. 映射路由

```csharp
var app = builder.Build();

app.UseWebSockets();   // ⚠️ 必须在 MapWebSocketHandler 之前调用

// 用默认配置
app.MapWebSocketHandler<ChatHandler>("/ws/chat");

// 或自定义配置
app.MapWebSocketHandler<ChatHandler>("/ws/chat", new WebSocketSessionOptions
{
    ReceiveBufferSize = 16384,
    MaxMessageSize = 4 * 1024 * 1024,
    SendQueueCapacity = 1000,
    ReceiveDispatchQueueCapacity = 1024,
    CloseTimeout = TimeSpan.FromSeconds(5),
    // 协议层保活 + 死链检测
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    KeepAliveTimeout = TimeSpan.FromSeconds(10)
});

app.Run();
```

> 忘记 `app.UseWebSockets()` 会导致升级握手失败；Handler 未注册到 DI 则 `MapWebSocketHandler` 会抛 `InvalidOperationException` 明确提示。

---

## 会话与广播

注册 `AddWebSocketSessionManager()` 后，可注入 `IWebSocketSessionManager` 操作全部在线连接：

```csharp
public interface IWebSocketSessionManager
{
    int Count { get; }                                    // 在线连接数
    IReadOnlyCollection<IWebSocketSession> GetAllSessions(); // 所有连接快照
    IWebSocketSession? GetSession(string id);             // 按 ID 取连接
    Task BroadcastAsync(ReadOnlyMemory<byte> message,
        WebSocketMessageType messageType = WebSocketMessageType.Text,
        CancellationToken cancellationToken = default);   // 广播
    Task BroadcastTextAsync(string text, CancellationToken cancellationToken = default);
}
```

```csharp
// 在任意服务中注入并使用
public class NotifyService(IWebSocketSessionManager sessions)
{
    public Task PushToAllAsync(string msg) => sessions.BroadcastTextAsync(msg);

    public Task PushToOneAsync(string sessionId, string msg)
        => sessions.GetSession(sessionId)?.SendTextAsync(msg) ?? Task.CompletedTask;

    public int Online => sessions.Count;
}
```

> 会话在中间件层自动注册/注销：连接进入即加入管理器，连接结束（无论正常还是异常）即移除，无需手动维护。
> 广播为**即发即弃**：`BroadcastTextAsync` 把消息入队到每个连接后即返回，不等待真正发送完成。

---

## 保活与死链检测

服务端的连接保活同样走 **WebSocket 协议层 Ping/Pong**，由运行时自动收发。通过两个选项启用：

| 选项 | 作用 |
|---|---|
| `KeepAliveInterval` | 运行时按此间隔发送协议层 PING |
| `KeepAliveTimeout`（.NET 8+） | 发送 PING 后超时无 PONG → 运行时 abort 连接 |

```csharp
app.MapWebSocketHandler<ChatHandler>("/ws/chat", new WebSocketSessionOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(15),
    KeepAliveTimeout = TimeSpan.FromSeconds(10)
});
```

为 `null` 时回退到 `app.UseWebSockets(new WebSocketOptions { KeepAliveInterval = ..., KeepAliveTimeout = ... })` 配置的**全局默认值**，因此你也可以在 `UseWebSockets` 处统一设置、各路径不再单独配置。连接死掉时运行时 abort → 接收循环抛异常 → 框架走清理流程并调用 `OnDisconnectedAsync`。

> **与本库客户端对接**：`ManagedWebSocketClient`（`EasilyNET.Core`）同样使用协议层保活。两端都设置 `KeepAliveInterval` + `KeepAliveTimeout` 即可，**无需在 `OnMessageAsync` 里处理任何心跳消息**。

> ⚠️ 协议层 Pong 由对端运行时自动回复，只证明"传输 + 运行时存活"。若要验证对端**业务循环**真的在处理消息，请在业务消息里自行附带探活（属普通业务逻辑）。

---

## 完整配置表

| 属性                           | 类型                            | 默认值      | 说明                                                       |
|------------------------------|-------------------------------|----------|----------------------------------------------------------|
| `SendQueueCapacity`          | `int`                         | 1000     | 每连接发送队列容量（满时发送背压等待）                                      |
| `ReceiveBufferSize`          | `int`                         | 16384    | 单次接收缓冲区大小（字节）                                            |
| `MaxMessageSize`             | `long`                        | 4MB      | 单条入站消息上限，超限以 `MessageTooBig` 断连防内存耗尽；设 0 或负数禁用（不推荐）      |
| `ReceiveDispatchQueueCapacity`| `int`                        | 1024     | 接收分发队列容量；解耦 `OnMessageAsync` 与接收循环，满时对接收侧背压             |
| `CloseTimeout`               | `TimeSpan`                    | 5 秒      | 关闭握手超时，防止对端不响应导致挂起                                       |
| `KeepAliveInterval`          | `TimeSpan?`                   | `null`   | **协议层** PING 间隔（接受连接时透传）；`null` 用 `UseWebSockets` 的全局默认 |
| `KeepAliveTimeout`           | `TimeSpan?`                   | `null`   | **协议层** PING/PONG 超时（.NET 8+）；与 `KeepAliveInterval` 同设即开启死链检测 |

---

## 核心组件

| 组件                         | 说明                                                                          |
|----------------------------|-----------------------------------------------------------------------------|
| `WebSocketHandler`         | 业务逻辑基类。重写 `OnConnected/OnMessage/OnDisconnected/OnError` 即可                  |
| `IWebSocketSession`        | 单个客户端连接。提供 `Id`、`State`、`Items` 及 `SendAsync/SendTextAsync/SendBinaryAsync/CloseAsync` |
| `IWebSocketSessionManager` | 会话管理器接口：`Count`、`GetAllSessions`、`GetSession`、`BroadcastAsync/BroadcastTextAsync` |
| `WebSocketSessionManager`  | 默认实现，线程安全，单例                                                                |
| `WebSocketSessionOptions`  | 每个映射路径的会话配置                                                                 |
| `MapWebSocketHandler<T>`   | 把 Handler 映射到路径的扩展方法                                                        |
| `AddWebSocketSessionManager` | 注册会话管理器的扩展方法                                                              |

`IWebSocketSession` 常用成员：

```csharp
string Id { get; }                        // Ulid，全局唯一、时间有序
WebSocketState State { get; }             // 底层连接状态
IDictionary<string, object?> Items { get; } // 按连接的线程安全数据袋
Task SendTextAsync(string text, ...);
Task SendBinaryAsync(byte[] bytes, ...);
Task SendAsync(ReadOnlyMemory<byte> message, WebSocketMessageType type, ...);
Task CloseAsync(WebSocketCloseStatus status, string? description, ...); // 主动关闭某连接
```

---

## 最佳实践

- **Handler 是共享单例**：不要用字段存单连接状态，按连接的数据放 `session.Items`；跨连接的共享状态需自行保证线程安全。
- **开启协议层保活**：生产环境建议设置 `KeepAliveInterval` + `KeepAliveTimeout`（或在 `UseWebSockets` 处全局设置），自动检测并清理死链。
- **回调里捕获异常**：未捕获的异常会走 `OnErrorAsync`，但建议在业务代码内自行处理关键路径。
- **鉴权放在 `OnConnectedAsync`**：因为它保证先于任何消息执行；鉴权失败可 `await session.CloseAsync(...)` 主动断开。
- **合理设置 `MaxMessageSize`**：对外暴露的服务务必保留上限以防滥用；需要大消息时同时调大客户端与服务端上限。

---

## 常见问题 FAQ

**Q：客户端连不上 / 升级握手失败？**
A：确认在 `MapWebSocketHandler` **之前**调用了 `app.UseWebSockets()`，且路径一致。

**Q：启动即抛"handler ... is not registered"？**
A：Handler 必须注册到 DI（`AddSingleton<T>()` 或 `[DependencyInjection]` 特性）后才能 `MapWebSocketHandler<T>`。

**Q：怎么自动清理掉线/僵死的连接？**
A：设置 `KeepAliveInterval` + `KeepAliveTimeout` 开启协议层死链检测；运行时 abort 后框架会触发清理与 `OnDisconnectedAsync`。

**Q：`OnMessageAsync` 会并发调用吗？**
A：同一连接内不会——按到达顺序串行。不同连接之间是并行的（各有独立循环）。

**Q：超大消息为什么连接被关了？**
A：超过 `MaxMessageSize`（默认 4MB）会以 `MessageTooBig` 主动关闭，调大该值或在客户端分块发送。
