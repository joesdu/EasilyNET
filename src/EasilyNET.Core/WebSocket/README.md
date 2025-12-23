# ManagedWebSocketClient

`ManagedWebSocketClient` 是一个封装了 `ClientWebSocket` 的高级客户端，旨在简化 WebSocket 的使用，并提供生产环境所需的健壮性功能。

## 功能特性

- **自动重连**: 内置指数退避（Exponential Backoff）策略，在连接断开时自动尝试重连。
- **心跳机制**: 自动发送心跳包（Ping），保持连接活跃，并检测死链。
- **高性能发送队列**: 使用 `System.Threading.Channels` 实现发送队列，支持高并发发送，避免阻塞。
- **线程安全**: 所有的公共方法都是线程安全的。
- **原生兼容**: 支持配置底层的 `ClientWebSocketOptions`，如设置 Headers、Proxy、Certificates 等。

## 使用示例

### 1. 初始化配置

```csharp
var options = new WebSocketClientOptions
{
    ServerUri = new Uri("ws://localhost:8080"),
    // 启用自动重连
    AutoReconnect = true,
    // 重连间隔
    ReconnectDelayMs = 1000,
    // 启用心跳
    HeartbeatEnabled = true,
    // 心跳间隔
    HeartbeatIntervalMs = 30000,
    // 配置原生 WebSocket 选项
    ConfigureWebSocket = socket =>
    {
        socket.Options.SetRequestHeader("Authorization", "Bearer my-token");
    }
};

using var client = new ManagedWebSocketClient(options);
```

### 2. 订阅事件

```csharp
// 收到消息
client.MessageReceived += (s, e) =>
{
    var message = Encoding.UTF8.GetString(e.Data.Span);
    Console.WriteLine($"收到消息: {message}");
};

// 状态变更
client.StateChanged += (s, e) =>
{
    Console.WriteLine($"状态变更: {e.PreviousState} -> {e.CurrentState}");
};

// 发生错误
client.Error += (s, e) =>
{
    Console.WriteLine($"发生错误: {e.Exception.Message}");
};

// 重连中
client.Reconnecting += (s, e) =>
{
    Console.WriteLine($"正在重连... 第 {e.AttemptNumber} 次尝试");
};
```

### 3. 连接与发送

```csharp
// 建立连接
await client.ConnectAsync();

// 发送文本消息
await client.SendTextAsync("Hello WebSocket");

// 发送二进制消息
byte[] data = [0x01, 0x02, 0x03];
await client.SendBinaryAsync(data);

// 断开连接
await client.DisconnectAsync();
```
