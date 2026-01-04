# WebSocket Server

基于 ASP.NET Core 的高性能 WebSocket 服务端实现。

## 功能特性

- **高性能**: 使用 `System.Threading.Channels` 实现发送队列，支持高并发发送，非阻塞。
- **低分配**: 使用 `ArrayPool<byte>` 和 `PooledMemoryStream` 优化内存使用，减少 GC 压力。
- **心跳机制**: 使用 `PeriodicTimer` 高效检测死链，可配置超时断开。
- **易用性**: 提供 `WebSocketHandler` 基类，只需关注业务逻辑。
- **集成**: 无缝集成 ASP.NET Core 中间件管道。
- **现代化 API**: 使用 `TimeSpan` 配置时间参数，符合 .NET 设计规范。

## 使用指南

### 1. 定义处理程序

继承 `WebSocketHandler` 并重写相关方法：

```csharp
public class ChatHandler : WebSocketHandler
{
    // 可以在这里通过构造函数注入所需的服务

    public override async Task OnConnectedAsync(IWebSocketSession session)
    {
        Console.WriteLine($"Client connected: {session.Id}");
        await session.SendTextAsync("Welcome to the chat!");
    }

    public override async Task OnDisconnectedAsync(IWebSocketSession session)
    {
        Console.WriteLine($"Client disconnected: {session.Id}");
    }

    public override async Task OnMessageAsync(IWebSocketSession session, WebSocketMessage message)
    {
        if (message.MessageType == WebSocketMessageType.Text)
        {
            var text = Encoding.UTF8.GetString(message.Data.Span);
            Console.WriteLine($"Received: {text}");

            // Echo back
            await session.SendTextAsync($"Echo: {text}");
        }
    }

    public override async Task OnErrorAsync(IWebSocketSession session, Exception exception)
    {
        Console.WriteLine($"Error on {session.Id}: {exception.Message}");
    }
}
```

### 2. 注册服务

在 `Program.cs` 中注册 Handler（通常为单例）：

```csharp
builder.Services.AddSingleton<ChatHandler>();
```

### 3. 映射路由

在 `Program.cs` 中配置中间件管道：

```csharp
var app = builder.Build();

app.UseWebSockets(); // 必须先启用 WebSockets

app.MapWebSocketHandler<ChatHandler>("/ws", options =>
{
    options.ReceiveBufferSize = 16384;           // 接收缓冲区大小
    options.SendQueueCapacity = 1000;            // 发送队列容量
    options.HeartbeatEnabled = true;             // 启用心跳
    options.HeartbeatInterval = TimeSpan.FromSeconds(30);  // 心跳间隔
    options.HeartbeatTimeout = TimeSpan.FromSeconds(10);   // 心跳超时
    options.CloseTimeout = TimeSpan.FromSeconds(5);        // 关闭超时
});

app.Run();
```

## 核心组件

- **IWebSocketSession**: 代表一个客户端连接，提供 `SendAsync`、`SendTextAsync`、`SendBinaryAsync` 等方法。
- **WebSocketHandler**: 业务逻辑基类，处理连接、断开、消息、错误事件。
- **WebSocketSessionOptions**: 会话配置选项，包括缓冲区大小、心跳设置等。
- **WebSocketMiddleware**: 处理 WebSocket 握手和会话生命周期。

## 性能特性

- **零分配接收**: 接收缓冲区从 `ArrayPool<byte>` 租借，避免频繁分配。
- **池化内存流**: 使用 `PooledMemoryStream` 组装大消息，减少内存碎片。
- **高效心跳**: 使用 `PeriodicTimer` 替代 `Task.Delay`，更高效且取消更及时。
- **内联优化**: 关键路径使用 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 优化。

## 配置说明

| 属性                      | 类型                          | 默认值 | 说明                   |
| ------------------------- | ----------------------------- | ------ | ---------------------- |
| `SendQueueCapacity`       | `int`                         | 1000   | 发送队列容量           |
| `ReceiveBufferSize`       | `int`                         | 16384  | 接收缓冲区大小（字节） |
| `HeartbeatEnabled`        | `bool`                        | `true` | 是否启用心跳           |
| `HeartbeatInterval`       | `TimeSpan`                    | 30 秒  | 心跳间隔               |
| `HeartbeatTimeout`        | `TimeSpan`                    | 10 秒  | 心跳超时               |
| `HeartbeatMessageFactory` | `Func<ReadOnlyMemory<byte>>?` | `null` | 心跳消息工厂           |
| `CloseTimeout`            | `TimeSpan`                    | 5 秒   | 关闭超时               |
