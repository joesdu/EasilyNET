namespace EasilyNET.Ipc.Server.Sample.Models;

/// <summary>
/// 用户实体
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// 创建用户负载
/// </summary>
public class CreateUserPayload
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
}

/// <summary>
/// 获取用户负载
/// </summary>
public class GetUserPayload
{
    public int UserId { get; set; }
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
}

/// <summary>
/// 删除用户负载
/// </summary>
public class DeleteUserPayload
{
    public int UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// 获取所有用户负载
/// </summary>
public class GetAllUsersPayload
{
    // 空负载，可能用于分页等扩展
}