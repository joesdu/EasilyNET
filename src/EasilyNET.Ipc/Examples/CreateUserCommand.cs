using EasilyNET.Ipc.Models;

namespace EasilyNET.Ipc.Examples;

/// <summary>
/// 示例：用户创建命令负载
/// </summary>
public class CreateUserPayload
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 邮箱
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// 年龄
    /// </summary>
    public int Age { get; set; }
}

/// <summary>
/// 示例：用户创建命令
/// </summary>
public class CreateUserCommand : IpcCommandBase<CreateUserPayload>
{
    /// <summary>
    /// 初始化新的用户创建命令
    /// </summary>
    /// <param name="payload">命令负载</param>
    /// <param name="targetId">目标标识符</param>
    public CreateUserCommand(CreateUserPayload payload, string? targetId = null)
        : base(payload, targetId) { }
}

/// <summary>
/// 示例：用户创建响应数据
/// </summary>
public class CreateUserResponse
{
    /// <summary>
    /// 用户 ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; }
}