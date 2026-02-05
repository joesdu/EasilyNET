# WebSocket Server

基于 ASP.NET Core 的高性能 WebSocket 服务端实现。

## 功能特性

- **高性能**: 使用 `System.Threading.Channels` 实现发送队列，支持高并发发送，非阻塞。
- **低分配**: 使用 `ArrayPool<byte>` 和 `PooledMemoryStream` 优化内存使用，减少 GC 压力。
- **心跳机制**: 使用 `PeriodicTimer` 高效检测死链，可配置超时断开。
- **会话管理**: 提供 `IWebSocketSessionManager` 支持会话跟踪和广播功能。
- **全局唯一 ID**: 使用 `Ulid` 生成全局唯一的会话标识符。
- **会话元数据**: 通过 `IWebSocketSession.Items` 存储会话级别的自定义数据。
- **易用性**: 提供 `WebSocketHandler` 基类，只需关注业务逻辑。
- **集成**: 无缝集成 ASP.NET Core 中间件管道。
- **现代化 API**: 使用 `TimeSpan` 配置时间参数，符合 .NET 设计规范。

## 使用指南

### 1. 定义处理程序

继承 `WebSocketHandler` 并重写相关方法：

```csharp
public class ChatHandler : WebSocketHandler
{
    private readonly IWebSocketSessionManager _sessionManager;

    public ChatHandler(IWebSocketSessionManager sessionManager)
    {
        _sessionManager = sessionManager;
    }

    public override async Task OnConnectedAsync(IWebSocketSession session)
    {
        Console.WriteLine($"Client connected: {session.Id}");
        
        // 存储会话级别的数据
        session.Items["ConnectedAt"] = DateTime.UtcNow;
        session.Items["Username"] = "Anonymous";
        
        await session.SendTextAsync("Welcome to the chat!");
        
        // 广播给所有其他用户
        await _sessionManager.BroadcastTextAsync($"User {session.Id} joined the chat!");
    }

    public override async Task OnDisconnectedAsync(IWebSocketSession session)
    {
        Console.WriteLine($"Client disconnected: {session.Id}");
        await _sessionManager.BroadcastTextAsync($"User {session.Id} left the chat!");
    }

    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(message.Data.Span);
            Console.WriteLine($"Received from {session.Id}: {text}");

            // 广播消息给所有连接的客户端
            await _sessionManager.BroadcastTextAsync($"[{session.Id}]: {text}");
        }
    }

    public override async Task OnErrorAsync(IWebSocketSession session, Exception exception)
    {
        Console.WriteLine($"Error on {session.Id}: {exception.Message}");
    }
}
```

### 2. 注册服务

在 `Program.cs` 中注册 Handler 和 SessionManager：

```csharp
// 注册会话管理器（可选，但推荐用于广播和会话跟踪）
builder.Services.AddWebSocketSessionManager();

// 注册 Handler（通常为单例）
builder.Services.AddSingleton<ChatHandler>();
```

### 3. 映射路由

在 `Program.cs` 中配置中间件管道：

```csharp
var app = builder.Build();

app.UseWebSockets(); // 必须先启用 WebSockets

app.MapWebSocketHandler<ChatHandler>("/ws", new WebSocketSessionOptions
{
    ReceiveBufferSize = 16384,                              // 接收缓冲区大小
    SendQueueCapacity = 1000,                               // 发送队列容量
    HeartbeatEnabled = true,                                // 启用心跳
    HeartbeatInterval = TimeSpan.FromSeconds(30),           // 心跳间隔
    HeartbeatTimeout = TimeSpan.FromSeconds(10),            // 心跳超时
    HeartbeatMessageType = WebSocketMessageType.Binary,     // 心跳消息类型
    CloseTimeout = TimeSpan.FromSeconds(5)                  // 关闭超时
});

app.Run();
```

## 核心组件

| 组件 | 说明 |
|------|------|
| `IWebSocketSession` | 代表一个客户端连接，提供 `SendAsync`、`SendTextAsync`、`SendBinaryAsync` 等方法，以及 `Items` 字典存储会话数据。 |
| `IWebSocketSessionManager` | 会话管理器接口，提供 `GetAllSessions()`、`GetSession(id)`、`BroadcastAsync()` 等方法。 |
| `WebSocketSessionManager` | 会话管理器默认实现，线程安全，支持会话跟踪和广播。 |
| `WebSocketHandler` | 业务逻辑基类，处理连接、断开、消息、错误事件。 |
| `WebSocketSessionOptions` | 会话配置选项，包括缓冲区大小、心跳设置等。 |

## 会话管理器 API

```csharp
public interface IWebSocketSessionManager
{
    // 获取活动会话数量
    int Count { get; }
    
    // 获取所有活动会话
    IReadOnlyCollection<IWebSocketSession> GetAllSessions();
    
    // 根据 ID 获取会话
    IWebSocketSession? GetSession(string id);
    
    // 向所有会话广播消息
    Task BroadcastAsync(ReadOnlyMemory<byte> message, WebSocketMessageType messageType = WebSocketMessageType.Text, CancellationToken cancellationToken = default);
    
    // 向所有会话广播文本消息
    Task BroadcastTextAsync(string text, CancellationToken cancellationToken = default);
}
```

## 性能特性

- **零分配接收**: 接收缓冲区从 `ArrayPool<byte>` 租借，避免频繁分配。
- **池化内存流**: 使用 `PooledMemoryStream` 组装大消息，减少内存碎片。
- **零拷贝消息传递**: 使用 `ToArraySegment()` 直接引用内部缓冲区，避免额外复制。
- **高效心跳**: 使用 `PeriodicTimer` 替代 `Task.Delay`，更高效且取消更及时。
- **内联优化**: 关键路径使用 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 优化。
- **全局唯一 ID**: 使用 `Ulid` 生成时间有序的全局唯一会话标识符。

## 配置说明

| 属性                      | 类型                          | 默认值   | 说明                                             |
| ------------------------- | ----------------------------- | -------- | ------------------------------------------------ |
| `SendQueueCapacity`       | `int`                         | 1000     | 发送队列容量                                     |
| `ReceiveBufferSize`       | `int`                         | 16384    | 接收缓冲区大小（字节）                           |
| `HeartbeatEnabled`        | `bool`                        | `true`   | 是否启用心跳                                     |
| `HeartbeatInterval`       | `TimeSpan`                    | 30 秒    | 心跳间隔                                         |
| `HeartbeatTimeout`        | `TimeSpan`                    | 10 秒    | 心跳超时                                         |
| `HeartbeatMessageType`    | `WebSocketMessageType`        | `Binary` | 心跳消息类型（Binary 兼容大多数客户端）          |
| `HeartbeatMessageFactory` | `Func<ReadOnlyMemory<byte>>?` | "ping"   | 心跳消息工厂，设为 null 禁用发送（仅超时检测）   |
| `CloseTimeout`            | `TimeSpan`                    | 5 秒     | 关闭超时                                         |

