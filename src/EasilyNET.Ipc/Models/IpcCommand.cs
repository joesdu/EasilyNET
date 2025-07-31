using EasilyNET.Core.Essentials;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// IPC 命令消息
/// </summary>
public sealed class IpcCommand
{
    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 命令类型（用户自定义，例如字符串或枚举）
    /// </summary>
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// 命令数据（序列化的字符串）
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// 目标标识符（可选，例如服务或资源的 ID）
    /// </summary>
    public string? TargetId { get; set; }

    /// <summary>
    /// 命令创建时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}