# EasilyNET.Ipc 测试项目完整代码

## 服务端项目 (EasilyNET.Ipc.Server.Sample)

### Program.cs
```csharp
using EasilyNET.Ipc;
using EasilyNET.Ipc.Server.Sample.Commands;
using EasilyNET.Ipc.Server.Sample.Handlers;
using EasilyNET.Ipc.Server.Sample.Models;
using EasilyNET.Ipc.Server.Sample.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

                // 注册用户相关命令处理器
                services.AddIpcCommandHandler<GetUserCommand, GetUserPayload, UserDto, GetUserHandler>();
                services.AddIpcCommandHandler<CreateUserCommand, CreateUserPayload, UserDto, CreateUserHandler>();
                services.AddIpcCommandHandler<UpdateUserCommand, UpdateUserPayload, UserDto, UpdateUserHandler>();
                services.AddIpcCommandHandler<DeleteUserCommand, DeleteUserPayload, bool, DeleteUserHandler>();

                // 注册订单相关命令处理器
                services.AddIpcCommandHandler<GetOrderCommand, GetOrderPayload, OrderDto, GetOrderHandler>();
                services.AddIpcCommandHandler<CreateOrderCommand, CreateOrderPayload, OrderDto, CreateOrderHandler>();
                services.AddIpcCommandHandler<UpdateOrderStatusCommand, UpdateOrderStatusPayload, OrderDto, UpdateOrderStatusHandler>();
                services.AddIpcCommandHandler<CancelOrderCommand, CancelOrderPayload, bool, CancelOrderHandler>();

                // 注册系统相关命令处理器
                services.AddIpcCommandHandler<GetSystemInfoCommand, GetSystemInfoPayload, SystemInfoDto, GetSystemInfoHandler>();
                services.AddIpcCommandHandler<HealthCheckCommand, HealthCheckPayload, HealthCheckResult, HealthCheckHandler>();
                services.AddIpcCommandHandler<NotificationCommand, NotificationPayload, bool, NotificationHandler>();
                services.AddIpcCommandHandler<EchoCommand, EchoPayload, string, EchoHandler>();

                // 批量注册命令类型
                services.RegisterIpcCommandsFromAssembly(typeof(Program).Assembly);
            });

        var host = builder.Build();

        // 添加优雅关闭处理
        var lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStarted.Register(() =>
        {
            Console.WriteLine("✅ IPC 服务端已启动");
            Console.WriteLine();
            Console.WriteLine("📝 支持的命令:");
            Console.WriteLine("   🧑 用户管理: GetUser, CreateUser, UpdateUser, DeleteUser");
            Console.WriteLine("   📦 订单处理: GetOrder, CreateOrder, UpdateOrderStatus, CancelOrder");
            Console.WriteLine("   🖥️ 系统监控: GetSystemInfo, HealthCheck, Notification, Echo");
            Console.WriteLine();
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
            Console.WriteLine($"详细错误信息: {ex}");
        }
    }
}
```

### Models/User.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

/// <summary>
/// 用户数据传输对象
/// </summary>
public class UserDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
    public UserRole Role { get; set; }
}

/// <summary>
/// 用户角色枚举
/// </summary>
public enum UserRole
{
    Guest = 0,
    User = 1,
    Admin = 2,
    SuperAdmin = 3
}

/// <summary>
/// 创建用户负载
/// </summary>
public class CreateUserPayload
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
}

/// <summary>
/// 获取用户负载
/// </summary>
public class GetUserPayload
{
    public int UserId { get; set; }
    public bool IncludeDetails { get; set; } = true;
}

/// <summary>
/// 更新用户负载
/// </summary>
public class UpdateUserPayload
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool? IsActive { get; set; }
    public UserRole? Role { get; set; }
}

/// <summary>
/// 删除用户负载
/// </summary>
public class DeleteUserPayload
{
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool SoftDelete { get; set; } = true;
}
```

### Models/Order.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

/// <summary>
/// 订单数据传输对象
/// </summary>
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
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Processing = 2,
    Shipped = 3,
    Delivered = 4,
    Cancelled = 5,
    Refunded = 6
}

/// <summary>
/// 创建订单负载
/// </summary>
public class CreateOrderPayload
{
    public int UserId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Quantity { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// 获取订单负载
/// </summary>
public class GetOrderPayload
{
    public int OrderId { get; set; }
    public bool IncludeUserInfo { get; set; } = false;
}

/// <summary>
/// 更新订单状态负载
/// </summary>
public class UpdateOrderStatusPayload
{
    public int OrderId { get; set; }
    public OrderStatus NewStatus { get; set; }
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// 取消订单负载
/// </summary>
public class CancelOrderPayload
{
    public int OrderId { get; set; }
    public string Reason { get; set; } = string.Empty;
}
```

### Models/SystemInfo.cs
```csharp
namespace EasilyNET.Ipc.Server.Sample.Models;

/// <summary>
/// 系统信息数据传输对象
/// </summary>
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
    public Dictionary<string, object> AdditionalInfo { get; set; } = new();
}

/// <summary>
/// 获取系统信息负载
/// </summary>
public class GetSystemInfoPayload
{
    public bool IncludePerformanceCounters { get; set; } = true;
    public bool IncludeMemoryInfo { get; set; } = true;
    public bool IncludeProcessInfo { get; set; } = false;
}

/// <summary>
/// 健康检查负载
/// </summary>
public class HealthCheckPayload
{
    public string ComponentName { get; set; } = string.Empty;
    public bool DeepCheck { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// 健康检查结果
/// </summary>
public class HealthCheckResult
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; set; } = new();
    public TimeSpan ResponseTime { get; set; }
    public DateTime CheckTime { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 通知负载
/// </summary>
public class NotificationPayload
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public DateTime? ScheduledTime { get; set; }
}

/// <summary>
/// 通知类型枚举
/// </summary>
public enum NotificationType
{
    Info = 0,
    Warning = 1,
    Error = 2,
    Success = 3,
    Debug = 4
}

/// <summary>
/// 回显负载
/// </summary>
public class EchoPayload
{
    public string Message { get; set; } = string.Empty;
    public int Count { get; set; } = 1;
    public TimeSpan Delay { get; set; } = TimeSpan.Zero;
}
```

继续创建命令、处理器和服务...