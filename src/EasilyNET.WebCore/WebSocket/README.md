# WebSocket Server

基于 ASP.NET Core 的高性能 WebSocket 服务端实现。

## 功能特性

- **高性能**: 使用 `System.Threading.Channels` 实现发送队列，支持高并发发送，非阻塞。
- **低分配**: 优化的内存使用，复用缓冲区。
- **易用性**: 提供 `WebSocketHandler` 基类，只需关注业务逻辑。
- **集成**: 无缝集成 ASP.NET Core 中间件管道。

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

app.MapWebSocketHandler<ChatHandler>("/ws");

app.Run();
```

## 核心组件

- **IWebSocketSession**: 代表一个客户端连接，提供 `SendAsync` 等方法。
- **WebSocketHandler**: 业务逻辑基类，处理连接、断开、消息事件。
- **WebSocketMiddleware**: 处理 WebSocket 握手和会话生命周期。
