namespace EasilyNET.Ipc.Abstractions;

/// <summary>
/// IPC 命令基础接口
/// </summary>
public interface IIpcCommandBase
{
    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// 目标标识符（可选，例如服务或资源的 ID）
    /// </summary>
    string? TargetId { get; }

    /// <summary>
    /// 命令创建时间
    /// </summary>
    DateTime Timestamp { get; }
}