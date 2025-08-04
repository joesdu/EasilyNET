# EasilyNET.Ipc

`EasilyNET.Ipc` 是一个现代化、高性能的跨平台进程间通信（IPC）库，专为 .NET 应用程序设计。它支持强类型命令、可靠的消息传递、以及灵活的序列化机制。

## ✨ 主要特性

- **🌍 跨平台支持**: Windows（命名管道）和 Linux（Unix 域套接字）
- **🔒 强类型通信**: 基于泛型的类型安全命令和响应
- **⚡ 高性能**: 支持多管道并发、连接池和高效序列化
- **🛡️ 可靠性**: 集成 Polly 重试策略和熔断器机制
- **🔧 灵活配置**: 支持代码配置和配置文件
- **📝 详细日志**: 内置 Microsoft.Extensions.Logging 支持
- **💉 依赖注入**: 原生支持 Microsoft.Extensions.DependencyInjection

## 📦 安装

```bash
dotnet add package EasilyNET.Ipc
```

**支持平台**: .NET 8.0+, Windows, Linux

## 🚀 快速开始

### 📖 基本概念

在使用 IPC 之前，需要了解几个核心概念：

1. **命令（Command）**: 实现 `IIpcCommand<TPayload>` 的类，表示要执行的操作
2. **处理器（Handler）**: 实现 `IIpcCommandHandler<TCommand, TPayload, TResponse>` 的类，处理特定命令
3. **客户端（Client）**: 发送命令到服务端的组件
4. **服务端（Server）**: 接收并处理命令的组件

### 1️⃣ 定义命令和响应

首先定义您的命令、负载和响应类型：

```csharp
using EasilyNET.Ipc.Abstractions;

// 定义命令负载
public class GetUserPayload
{
    public int UserId { get; set; }
    public bool IncludeProfile { get; set; }
}

// 定义响应数据
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// 定义命令
public class GetUserCommand : IIpcCommand<GetUserPayload>
{
    public GetUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

### 2️⃣ 实现命令处理器

```csharp
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;

public class GetUserHandler : IIpcCommandHandler<GetUserCommand, GetUserPayload, UserDto>
{
    private readonly IUserService _userService;
    private readonly ILogger<GetUserHandler> _logger;

    public GetUserHandler(IUserService userService, ILogger<GetUserHandler> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<IpcCommandResponse<UserDto>> HandleAsync(
        GetUserCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("处理获取用户命令: UserId={UserId}", command.Payload.UserId);

            var user = await _userService.GetUserAsync(command.Payload.UserId, cancellationToken);

            if (user == null)
            {
                return IpcCommandResponse<UserDto>.CreateFailure(
                    command.CommandId,
                    $"用户 {command.Payload.UserId} 不存在"
                );
            }

            var userDto = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                CreatedAt = user.CreatedAt
            };

            return IpcCommandResponse<UserDto>.CreateSuccess(command.CommandId, userDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理获取用户命令时发生错误");
            return IpcCommandResponse<UserDto>.CreateFailure(command.CommandId, ex.Message);
        }
    }
}
```

### 3️⃣ 服务端配置

```csharp
using EasilyNET.Ipc;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((context, services) =>
{
    // 注册 IPC 服务端
    services.AddIpcServer(options =>
    {
        options.PipeName = "MyApp_IPC";
        options.TransportCount = 4;
        options.DefaultTimeout = TimeSpan.FromSeconds(30);
    });

    // 注册命令处理器
    services.AddIpcCommandHandler<GetUserCommand, GetUserPayload, UserDto, GetUserHandler>();

    // 注册业务服务
    services.AddScoped<IUserService, UserService>();
});

var host = builder.Build();
await host.RunAsync();
```

### 4️⃣ 客户端配置

```csharp
using EasilyNET.Ipc;
using EasilyNET.Ipc.Interfaces;

var services = new ServiceCollection();

// 注册 IPC 客户端
services.AddIpcClient(options =>
{
    options.PipeName = "MyApp_IPC";
    options.DefaultTimeout = TimeSpan.FromSeconds(15);
    options.RetryPolicy.MaxAttempts = 3;
});

// 注册命令类型（客户端需要）
services.RegisterIpcCommand<GetUserCommand>();

var provider = services.BuildServiceProvider();
```

### 5️⃣ 发送命令

```csharp
public class UserController : ControllerBase
{
    private readonly IIpcClient _ipcClient;

    public UserController(IIpcClient ipcClient)
    {
        _ipcClient = ipcClient;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var command = new GetUserCommand
        {
            Payload = new GetUserPayload
            {
                UserId = id,
                IncludeProfile = true
            }
        };

        try
        {
            var response = await _ipcClient.SendAsync<UserDto>(command);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"IPC 调用失败: {ex.Message}");
        }
    }
}
```

## ⚙️ 高级配置

### 配置文件方式

在 `appsettings.json` 中定义配置：

```json
{
  "Ipc": {
    "PipeName": "MyApp_IPC",
    "TransportCount": 4,
    "MaxServerInstances": 4,
    "ClientPipePoolSize": 2,
    "DefaultTimeout": "00:00:30",
    "RetryPolicy": {
      "MaxAttempts": 5,
      "InitialDelay": "00:00:01",
      "BackoffType": "Exponential",
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.5,
      "MinimumThroughput": 5,
      "BreakDuration": "00:00:30"
    },
    "Timeout": {
      "Ipc": "00:00:10",
      "Business": "00:00:30"
    }
  }
}
```

然后在代码中使用：

```csharp
// 服务端
services.AddIpcServer(configuration);

// 客户端
services.AddIpcClient(configuration);
```

### 批量注册命令

```csharp
// 注册程序集中的所有命令
services.RegisterIpcCommandsFromAssembly(
    Assembly.GetExecutingAssembly(),
    typeof(GetUserCommand).Assembly
);
```

### 自定义序列化器

实现 `IIpcGenericSerializer` 接口：

```csharp
public class MessagePackIpcSerializer : IIpcGenericSerializer
{
    public byte[] Serialize<T>(T obj) => MessagePackSerializer.Serialize(obj);

    public T? Deserialize<T>(byte[] data) => MessagePackSerializer.Deserialize<T>(data);

    // 实现其他接口方法...
}
```

注册自定义序列化器：

```csharp
services.AddIpcServer(options =>
{
    options.Serializer = new MessagePackIpcSerializer();
});
```

## 📊 性能优化建议

1. **合理设置传输数量**: `TransportCount` 应根据并发需求调整
2. **使用连接池**: `ClientPipePoolSize` 可以减少连接创建开销
3. **选择高效序列化器**: MessagePack 通常比 JSON 更快
4. **调整超时设置**: 根据实际网络环境和处理时间调整
5. **启用压缩**: 对于大数据传输，考虑启用压缩

## 🔧 故障排除

### 常见问题

**1. 连接失败**

```
错误: Pipe is broken
解决: 检查服务端是否启动，确保 PipeName 一致
```

**2. 超时错误**

```
错误: Operation timed out
解决: 增加 DefaultTimeout 或 Business Timeout 值
```

**3. 序列化错误**

```
错误: Cannot deserialize
解决: 确保客户端和服务端使用相同的序列化器和数据结构
```

### 调试技巧

1. **启用详细日志**:

```csharp
builder.ConfigureLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
});
```

2. **检查连接状态**:

```csharp
// 在发送命令前检查连接
if (!_ipcClient.IsConnected)
{
    await _ipcClient.ConnectAsync();
}
```

## 📝 API 参考

### 核心接口

- **`IIpcClient`**: IPC 客户端接口
- **`IIpcCommandHandler<TCommand, TPayload, TResponse>`**: 命令处理器接口
- **`IIpcCommand<TPayload>`**: 命令接口
- **`IIpcGenericSerializer`**: 序列化器接口

### 扩展方法

- **`AddIpcServer()`**: 注册 IPC 服务端
- **`AddIpcClient()`**: 注册 IPC 客户端
- **`AddIpcCommandHandler<>()`**: 注册命令处理器
- **`RegisterIpcCommand<>()`**: 注册单个命令
- **`RegisterIpcCommandsFromAssembly()`**: 批量注册命令

## 📜 许可证

MIT License

## 🤝 贡献

欢迎提交 Issues 和 Pull Requests！请确保：

1. 遵循现有的代码风格
2. 添加必要的单元测试
3. 更新相关文档
4. 测试跨平台兼容性

---

**注意**: 此文档基于最新的 EasilyNET.Ipc 架构编写。如果您在使用过程中遇到问题，请检查是否使用了最新版本的库。
