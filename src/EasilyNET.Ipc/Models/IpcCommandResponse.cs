namespace EasilyNET.Ipc.Models;

/// <summary>
/// IPC 命令响应
/// </summary>
public sealed class IpcCommandResponse
{
    /// <summary>
    /// 对应的命令 ID
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据（序列化的字符串）
    /// </summary>
    public string? Data { get; set; }

    /// <summary>
    /// 响应消息（成功或错误描述）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}