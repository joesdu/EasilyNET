using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Interfaces;

namespace EasilyNET.Ipc.Client.Sample.Commands;

#region Echo Command

/// <summary>
/// 简单的测试命令
/// </summary>
public class EchoCommand : IIpcCommand<string>
{
    /// <summary>
    /// 命令负载数据
    /// </summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 目标标识符
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 命令创建时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion

#region User Commands

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

/// <summary>
/// 创建用户命令
/// </summary>
public class CreateUserCommand : IIpcCommand<CreateUserPayload>
{
    public CreateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 获取用户命令
/// </summary>
public class GetUserCommand : IIpcCommand<GetUserPayload>
{
    public GetUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 更新用户命令
/// </summary>
public class UpdateUserCommand : IIpcCommand<UpdateUserPayload>
{
    public UpdateUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 删除用户命令
/// </summary>
public class DeleteUserCommand : IIpcCommand<DeleteUserPayload>
{
    public DeleteUserPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 获取所有用户命令
/// </summary>
public class GetAllUsersCommand : IIpcCommand<object?>
{
    public object? Payload { get; set; }
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion

#region Math Commands

/// <summary>
/// 数学计算负载
/// </summary>
public class MathCalculationPayload
{
    public double Number1 { get; set; }
    public double Number2 { get; set; }
    public string Operation { get; set; } = string.Empty; // +, -, *, /
}

/// <summary>
/// 数学计算结果
/// </summary>
public class MathResult
{
    public double Result { get; set; }
    public string Operation { get; set; } = string.Empty;
    public double Number1 { get; set; }
    public double Number2 { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 数学计算命令
/// </summary>
public class MathCalculationCommand : IIpcCommand<MathCalculationPayload>
{
    public MathCalculationPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion

#region Delay Commands

/// <summary>
/// 延时处理负载
/// </summary>
public class DelayProcessPayload
{
    public int DelaySeconds { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 延时处理结果
/// </summary>
public class DelayResult
{
    public string Message { get; set; } = string.Empty;
    public int DelaySeconds { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double ActualDelayMs { get; set; }
}

/// <summary>
/// 延时处理命令
/// </summary>
public class DelayProcessCommand : IIpcCommand<DelayProcessPayload>
{
    public DelayProcessPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion

#region Error Test Commands

/// <summary>
/// 错误测试负载
/// </summary>
public class ErrorTestPayload
{
    public string ErrorType { get; set; } = string.Empty; // ArgumentException, InvalidOperation, Custom
    public string? CustomMessage { get; set; }
}

/// <summary>
/// 错误测试命令
/// </summary>
public class ErrorTestCommand : IIpcCommand<ErrorTestPayload>
{
    public ErrorTestPayload Payload { get; set; } = new();
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion

#region Status Commands

/// <summary>
/// 服务器状态
/// </summary>
public class ServerStatus
{
    public DateTime StartTime { get; set; }
    public TimeSpan Uptime { get; set; }
    public int ProcessedCommands { get; set; }
    public int ActiveConnections { get; set; }
    public string ServerVersion { get; set; } = string.Empty;
    public Dictionary<string, object> Statistics { get; set; } = new();
}

/// <summary>
/// 获取服务器状态命令
/// </summary>
public class GetServerStatusCommand : IIpcCommand<object?>
{
    public object? Payload { get; set; }
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();
    public string? TargetId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

#endregion
