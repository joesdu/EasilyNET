# ManagedWebSocketClient

`ManagedWebSocketClient` 是一个封装了 `ClientWebSocket` 的高级客户端，旨在简化 WebSocket 的使用，并提供生产环境所需的健壮性功能。

## 功能特性

- **自动重连**: 内置指数退避（Exponential Backoff）策略，在连接断开时自动尝试重连。
- **心跳机制**: 使用 `PeriodicTimer` 高效发送心跳包（Ping），保持连接活跃，并检测死链。
- **高性能发送队列**: 使用 `System.Threading.Channels` 实现发送队列，支持高并发发送，避免阻塞。
- **内存池优化**: 使用 `ArrayPool<byte>` 和 `PooledMemoryStream` 减少 GC 压力。
- **线程安全**: 所有的公共方法都是线程安全的。
- **原生兼容**: 支持配置底层的 `ClientWebSocketOptions`，如设置 Headers、Proxy、Certificates 等。
- **现代化 API**: 使用 `TimeSpan` 替代毫秒配置，更符合 .NET 设计规范。

## 使用示例

### 1. 初始化配置

```csharp
var options = new WebSocketClientOptions
{
    ServerUri = new Uri("ws://localhost:8080"),
    // 启用自动重连
    AutoReconnect = true,
    // 重连间隔 (使用 TimeSpan)
    ReconnectDelay = TimeSpan.FromSeconds(1),
    // 最大重连延迟
    MaxReconnectDelay = TimeSpan.FromSeconds(30),
    // 启用心跳
    HeartbeatEnabled = true,
    // 心跳间隔
    HeartbeatInterval = TimeSpan.FromSeconds(30),
    // 心跳超时
    HeartbeatTimeout = TimeSpan.FromSeconds(10),
    // 连接超时
    ConnectionTimeout = TimeSpan.FromSeconds(10),
    // 配置原生 WebSocket 选项
    ConfigureWebSocket = socket =>
    {
        socket.Options.SetRequestHeader("Authorization", "Bearer my-token");
    }
};

await using var client = new ManagedWebSocketClient(options);
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
    Console.WriteLine($"发生错误 [{e.Context}]: {e.Exception.Message}");
};

// 重连中
client.Reconnecting += (s, e) =>
{
    Console.WriteLine($"正在重连... 第 {e.AttemptNumber} 次尝试，延迟 {e.Delay.TotalSeconds} 秒");
    // 可通过设置 e.Cancel = true 取消重连
};

// 连接关闭
client.Closed += (s, e) =>
{
    Console.WriteLine($"连接关闭: {e.CloseDescription} (由客户端发起: {e.InitiatedByClient})");
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

// 发送 ReadOnlyMemory<byte>
await client.SendBinaryAsync(data.AsMemory());

// 断开连接
await client.DisconnectAsync();
```

## 性能特性

- **零分配接收**: 接收缓冲区从 `ArrayPool<byte>` 租借，避免频繁分配。
- **池化内存流**: 使用 `PooledMemoryStream` 组装大消息，减少内存碎片。
- **高效心跳**: 使用 `PeriodicTimer` 替代 `Task.Delay`，更高效且取消更及时。
- **非阻塞心跳**: 心跳消息通过 `TryWrite` 入队，队列满时跳过本次心跳而非阻塞，避免影响心跳循环。
- **内联优化**: 关键路径使用 `[MethodImpl(MethodImplOptions.AggressiveInlining)]` 优化。

## 配置说明

| 属性                    | 类型       | 默认值 | 说明                      |
| ----------------------- | ---------- | ------ | ------------------------- |
| `ServerUri`             | `Uri?`     | `null` | WebSocket 服务器地址      |
| `AutoReconnect`         | `bool`     | `true` | 是否启用自动重连          |
| `MaxReconnectAttempts`  | `int`      | `5`    | 最大重连次数，-1 表示无限 |
| `ReconnectDelay`        | `TimeSpan` | 1 秒   | 初始重连延迟              |
| `MaxReconnectDelay`     | `TimeSpan` | 30 秒  | 最大重连延迟              |
| `UseExponentialBackoff` | `bool`     | `true` | 是否使用指数退避          |
| `HeartbeatEnabled`      | `bool`     | `true` | 是否启用心跳              |
| `HeartbeatInterval`     | `TimeSpan` | 30 秒  | 心跳间隔                  |
| `HeartbeatTimeout`      | `TimeSpan` | 10 秒  | 心跳超时                  |
| `ConnectionTimeout`     | `TimeSpan` | 10 秒  | 连接超时                  |
| `ReceiveBufferSize`     | `int`      | 16384  | 接收缓冲区大小            |
| `SendQueueCapacity`     | `int`      | 1000   | 发送队列容量              |
| `WaitForSendCompletion` | `bool`     | `true` | 是否等待发送完成          |
