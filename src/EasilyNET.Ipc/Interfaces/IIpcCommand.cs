namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// IPC 命令基础接口，所有 IPC 命令都应实现此接口
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

/// <summary>
/// IPC 泛型命令接口
/// </summary>
/// <typeparam name="TPayload">负载数据类型</typeparam>
public interface IIpcCommand<out TPayload> : IIpcCommandBase
{
    /// <summary>
    /// 命令负载数据
    /// </summary>
    TPayload Payload { get; }
}