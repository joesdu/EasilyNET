# EasilyNET.Ipc 完整测试项目代码

## 项目 1: EasilyNET.Ipc.Server.Sample

### 1. 项目文件 (EasilyNET.Ipc.Server.Sample.csproj)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <AssemblyName>EasilyNET.Ipc.Server.Sample</AssemblyName>
    <RootNamespace>EasilyNET.Ipc.Server.Sample</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Logging.Console" Version="9.0.0" />
    <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\EasilyNET.Ipc\EasilyNET.Ipc.csproj" />
    <ProjectReference Include="..\..\src\EasilyNET.Core\EasilyNET.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="appsettings.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </None>
  </ItemGroup>
</Project>
```

### 2. 配置文件 (appsettings.json)
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "EasilyNET.Ipc": "Debug",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "Ipc": {
    "PipeName": "EasilyNET_IPC_Test",
    "UnixSocketPath": "/tmp/easilynet_ipc_test.sock", 
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

### 3. 主程序 (Program.cs)
```csharp
using EasilyNET.Ipc;
using EasilyNET.Ipc.Server.Sample.Commands;
using EasilyNET.Ipc.Server.Sample.Handlers;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Ipc.Server.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EasilyNET.Ipc.Server.Sample;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== EasilyNET.Ipc Server Sample ===");
        Console.WriteLine("正在启动 IPC 服务端...");

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                // 注册 IPC 服务端
                services.AddIpcServer(context.Configuration);

                // 注册业务服务
                services.AddSingleton<IUserService, UserService>();
                services.AddSingleton<IOrderService, OrderService>();
                services.AddSingleton<ISystemService, SystemService>();

                // 注册命令处理器
                services.AddIpcCommandHandler<GetUserCommand, GetUserPayload, UserDto, GetUserHandler>();
                services.AddIpcCommandHandler<CreateUserCommand, CreateUserPayload, UserDto, CreateUserHandler>();
                services.AddIpcCommandHandler<UpdateUserCommand, UpdateUserPayload, UserDto, UpdateUserHandler>();
                services.AddIpcCommandHandler<DeleteUserCommand, DeleteUserPayload, bool, DeleteUserHandler>();

                services.AddIpcCommandHandler<GetOrderCommand, GetOrderPayload, OrderDto, GetOrderHandler>();
                services.AddIpcCommandHandler<CreateOrderCommand, CreateOrderPayload, OrderDto, CreateOrderHandler>();
                services.AddIpcCommandHandler<UpdateOrderStatusCommand, UpdateOrderStatusPayload, OrderDto, UpdateOrderStatusHandler>();
                services.AddIpcCommandHandler<CancelOrderCommand, CancelOrderPayload, bool, CancelOrderHandler>();

                services.AddIpcCommandHandler<GetSystemInfoCommand, GetSystemInfoPayload, SystemInfoDto, GetSystemInfoHandler>();
                services.AddIpcCommandHandler<HealthCheckCommand, HealthCheckPayload, HealthCheckResult, HealthCheckHandler>();
                services.AddIpcCommandHandler<NotificationCommand, NotificationPayload, bool, NotificationHandler>();

                // 批量注册命令类型
                services.RegisterIpcCommandsFromAssembly(typeof(Program).Assembly);
            });

        var host = builder.Build();

        // 添加优雅关闭处理
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("✅ IPC 服务端已启动");
            Console.WriteLine("📝 支持的命令:");
            Console.WriteLine("   - 用户管理: GetUser, CreateUser, UpdateUser, DeleteUser");
            Console.WriteLine("   - 订单处理: GetOrder, CreateOrder, UpdateOrderStatus, CancelOrder");
            Console.WriteLine("   - 系统监控: GetSystemInfo, HealthCheck, Notification");
            Console.WriteLine("⏹️  按 Ctrl+C 停止服务");
        });

        lifetime.ApplicationStopping.Register(() =>
        {
            Console.WriteLine("🛑 正在停止 IPC 服务端...");
        });

        lifetime.ApplicationStopped.Register(() =>
        {
            Console.WriteLine("✅ IPC 服务端已停止");
        });

        try
        {
            await host.RunAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 服务启动失败: {ex.Message}");
        }
    }
}
```

### 4. 数据模型 (Models/)

#### Models/User.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class CreateUserPayload
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
}

public class GetUserPayload
{
    public int UserId { get; set; }
    public bool IncludeDetails { get; set; } = true;
}

public class UpdateUserPayload
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? IsActive { get; set; }
}

public class DeleteUserPayload
{
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

#### Models/Order.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

public class OrderDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5
}

public class CreateOrderPayload
{
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
}

public class GetOrderPayload
{
    public int OrderId { get; set; }
    public bool IncludeUserInfo { get; set; } = false;
}

public class UpdateOrderStatusPayload
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CancelOrderPayload
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

#### Models/SystemInfo.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

public class SystemInfoDto
{
    public string MachineName { get; set; } = string.Empty;
    public string OperatingSystem { get; set; } = string.Empty;
    public string ProcessorCount { get; set; } = string.Empty;
    public long TotalMemory { get; set; }
    public long AvailableMemory { get; set; }
    public double CpuUsage { get; set; }
    public TimeSpan Uptime { get; set; }
    public DateTime ServerTime { get; set; }
    public string Version { get; set; } = string.Empty;
}

public class GetSystemInfoPayload
{
    public bool IncludePerformanceCounters { get; set; } = true;
    public bool IncludeMemoryInfo { get; set; } = true;
}

public class HealthCheckPayload
{
    public string ComponentName { get; set; } = string.Empty;
    public bool DeepCheck { get; set; } = false;
}

public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckTime { get; set; }
}

public class NotificationPayload
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

public enum NotificationType
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Success = 3
}
```

### 5. 命令定义 (Commands/)

#### Commands/UserCommands.cs
```csharp
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Core.Essentials;

namespace EasilyNET.Ipc.Server.Sample.Commands;

public class GetUserCommand : IIpcCommand<GetUserPayload>
{
    public GetUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CreateUserCommand : IIpcCommand<CreateUserPayload>
{
    public CreateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class UpdateUserCommand : IIpcCommand<UpdateUserPayload>
{
    public UpdateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DeleteUserCommand : IIpcCommand<DeleteUserPayload>
{
    public DeleteUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

#### Commands/OrderCommands.cs
```csharp
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Core.Essentials;

namespace EasilyNET.Ipc.Server.Sample.Commands;

public class GetOrderCommand : IIpcCommand<GetOrderPayload>
{
    public GetOrderPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CreateOrderCommand : IIpcCommand<CreateOrderPayload>
{
    public CreateOrderPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class UpdateOrderStatusCommand : IIpcCommand<UpdateOrderStatusPayload>
{
    public UpdateOrderStatusPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class CancelOrderCommand : IIpcCommand<CancelOrderPayload>
{
    public CancelOrderPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

#### Commands/SystemCommands.cs
```csharp
using EasilyNET.Ipc.Interfaces;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Core.Essentials;

namespace EasilyNET.Ipc.Server.Sample.Commands;

public class GetSystemInfoCommand : IIpcCommand<GetSystemInfoPayload>
{
    public GetSystemInfoPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class HealthCheckCommand : IIpcCommand<HealthCheckPayload>
{
    public HealthCheckPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class NotificationCommand : IIpcCommand<NotificationPayload>
{
    public NotificationPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
```

继续下一部分...