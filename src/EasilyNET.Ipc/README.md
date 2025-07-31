#### EasilyNET.Ipc

`EasilyNET.Ipc` 是一个高性能、跨平台的进程间通信（IPC）库,支持 Windows（命名管道）和 Linux（Unix 域套接字）.它提供双向通信、多管道并发支持以及可插拔的序列化器,适用于各种业务场景.

## 功能特性

- **跨平台支持**:在 Windows 上使用命名管道,在 Linux 上使用 Unix 域套接字.
- **双向通信**:通过单一连接发送命令并接收响应,简化通信流程.
- **多管道支持**:服务端支持多个管道/套接字实例,客户端维护传输池,提高并发性能.
- **可插拔序列化器**:默认支持 JSON,扩展支持 MessagePack,用户可自定义序列化器.
- **健壮性**:集成 Polly 重试和断路器策略,处理网络中断和超时.
- **依赖注入**:通过 `IServiceCollection` 提供简单集成.
- **详细日志**:支持 Microsoft.Extensions.Logging,便于调试和监控.

## 安装

1. **NuGet 包**:
   ```bash
   dotnet add package EasilyNET.Ipc
   ```

2. **支持的平台**:
   - .NET 8.0+
   - Windows、Linux

## 快速开始

### 服务端

1. **实现自定义命令处理器**:

```csharp
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using System.Text.Json;

public class CustomCommandHandler : IIpcCommandHandler
{
    public async Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command)
    {
        var response = new IpcCommandResponse { CommandId = command.CommandId };
        try
        {
            switch (command.CommandType)
            {
                case "Echo":
                    response.Success = true;
                    response.Data = command.Payload;
                    response.Message = "Echo command processed";
                    break;
                case "Calculate":
                    if (!string.IsNullOrEmpty(command.Payload))
                    {
                        var numbers = JsonSerializer.Deserialize<int[]>(command.Payload);
                        if (numbers != null)
                        {
                            response.Success = true;
                            response.Data = JsonSerializer.Serialize(numbers.Sum());
                            response.Message = "Calculation completed";
                        }
                    }
                    break;
                default:
                    response.Success = false;
                    response.Message = $"Unknown command type: {command.CommandType}";
                    break;
            }
        }
        catch (Exception ex)
        {
            response.Success = false;
            response.Message = $"Error processing command: {ex.Message}";
        }
        return response;
    }
}
```

2. **注册服务**:

```csharp
using EasilyNET.Ipc.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);
builder.ConfigureServices((hostContext, services) =>
{
    services.AddIpcServer(hostContext.Configuration);
    services.AddSingleton<IIpcCommandHandler, CustomCommandHandler>();
});

var host = builder.Build();
await host.RunAsync();
```

### 客户端

```csharp
using EasilyNET.Ipc.Extensions;
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

var services = new ServiceCollection();
services.AddIpcClient();
var provider = services.BuildServiceProvider();

var client = provider.GetRequiredService<IIpcClient>();

var command = new IpcCommand
{
    CommandType = "Echo",
    Payload = JsonSerializer.Serialize("Hello, IPC!")
};

var response = await client.SendCommandAsync(command);
if (response?.Success == true)
{
    Console.WriteLine($"Response: {response.Data}, Message: {response.Message}");
}
else
{
    Console.WriteLine($"Error: {response?.Message}");
}

client.Dispose();
```

## 配置

配置通过 `IpcOptions` 类进行,推荐在 `appsettings.json` 中定义:

```json
{
  "Ipc": {
    "PipeName": "MyIpcPipe",
    "UnixSocketPath": "/tmp/myipc.sock",
    "MaxServerInstances": 4,
    "ClientPipePoolSize": 2,
    "RetryPolicy": {
      "MaxAttempts": 5,
      "InitialDelay": "00:00:01",
      "BackoffType": "Exponential",
      "UseJitter": true
    },
    "CircuitBreaker": {
      "FailureRatio": 0.8,
      "MinimumThroughput": 5,
      "BreakDuration": "00:00:30"
    },
    "Timeout": {
      "Ipc": "00:00:10",
      "Business": "00:00:30"
    },
    "DefaultTimeout": "00:00:30"
  }
}
```

加载配置:

```csharp
services.Configure<IpcOptions>(configuration.GetSection(IpcOptions.SectionName));
```

### 配置项说明

- `PipeName`:Windows 命名管道名称.
- `UnixSocketPath`:Linux Unix 域套接字路径.
- `MaxServerInstances`:服务端最大管道/套接字实例数.
- `ClientPipePoolSize`:客户端传输池大小.
- `Serializer`:序列化器类型,需使用代码中进行配置,可继承 `IIpcSerializer` 接口后自定义序列化器.
- `RetryPolicy`:重试策略配置.
- `CircuitBreaker`:断路器配置.
- `Timeout`:超时设置.

## 自定义序列化器

实现 `IIpcSerializer` 接口以支持自定义序列化:

```csharp
public class CustomSerializer : IIpcSerializer
{
    public byte[] SerializeCommand(IpcCommand command) { /* 实现 */ }
    public IpcCommand? DeserializeCommand(byte[] data) { /* 实现 */ }
    public byte[] SerializeResponse(IpcCommandResponse response) { /* 实现 */ }
    public IpcCommandResponse? DeserializeResponse(byte[] data) { /* 实现 */ }
}
```

注册自定义序列化器:

### 方式一: 通过代码配置
```csharp
// 配置服务端 
services.AddIpcServer(options => { options.Serializer = new CustomSerializer(); });
// 配置客户端 
services.AddIpcClient(options => { options.Serializer = new CustomSerializer(); });
```

### 方式二: 通过配置文件和代码结合

```csharp
// 先加载配置文件，再覆盖序列化器 
services.AddIpcServer(configuration, options => { options.Serializer = new MessagePackIpcSerializer(); });
```

### 方式三: 在 IpcOptions 配置中直接设置

```csharp
services.Configure<IpcOptions>(options => { options.Serializer = new CustomSerializer(); }); 
services.AddIpcServer();
```

**注意**: 序列化器必须在代码中进行配置，不能通过 JSON 配置文件设置。这是因为序列化器是具体的对象实例，而 JSON 配置文件只能存储基本数据类型。


## API 参考

### IIpcClient

- `Task<IpcCommandResponse?> SendCommandAsync(IpcCommand command, TimeSpan timeout = default)`:发送命令并接收响应.

### IIpcCommandHandler

- `Task<IpcCommandResponse> HandleCommandAsync(IpcCommand command)`:处理接收到的命令.

### IIpcCommandService

- `Task<IpcCommandResponse?> SendAndReceiveAsync(IpcCommand command, TimeSpan timeout = default)`:发送命令并接收响应.

### IIpcSerializer

- `byte[] SerializeCommand(IpcCommand command)`:序列化命令.
- `IpcCommand? DeserializeCommand(byte[] data)`:反序列化命令.
- `byte[] SerializeResponse(IpcCommandResponse response)`:序列化响应.
- `IpcCommandResponse? DeserializeResponse(byte[] data)`:反序列化响应.

### IIpcTransport

- `Task WaitForConnectionAsync(CancellationToken cancellationToken)`:服务端等待连接.
- `Task ConnectAsync(TimeSpan timeout, CancellationToken cancellationToken)`:客户端连接.
- `Task<byte[]> ReadAsync(CancellationToken cancellationToken)`:读取数据.
- `Task WriteAsync(byte[] data, CancellationToken cancellationToken)`:写入数据.
- `bool IsConnected`:检查连接状态.
- `void Disconnect()`:断开连接.

## 性能优化

- **双向管道**:通过单一连接传递命令和响应,减少开销.
- **多管道**:服务端支持多个实例,客户端使用传输池,提高并发能力.
- **MessagePack**:使用 MessagePack 序列化器可显著降低数据大小和序列化时间.
- **Polly 策略**:重试和断路器确保通信健壮性.

## 故障排除

- **管道断开（Pipe is broken）**:
  - 检查服务端是否已启动.
  - 确保 `MaxServerInstances` 足够支持并发连接.
  - 增加 `RetryPolicy.InitialDelay` 和 `MaxAttempts`.

- **超时**:
  - 调整 `Timeout.Ipc` 和 `Timeout.Business`.
  - 检查命令处理逻辑是否耗时过长.

- **序列化错误**:
  - 确保 `Payload` 和 `Data` 字段与序列化器兼容.
  - 使用 JSON 序列化器进行调试.

## 许可证

MIT License

## 贡献

欢迎提交 issues 和 pull requests！请确保代码符合 .NET 8 标准,并包含单元测试.