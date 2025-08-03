using EasilyNET.Core.Essentials;
using EasilyNET.Ipc.Abstractions;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// IPC 命令基础实现
/// </summary>
/// <typeparam name="TPayload">负载数据类型</typeparam>
public abstract class IpcCommandBase<TPayload> : IIpcCommand<TPayload>
{
    /// <summary>
    /// 初始化新的 IPC 命令实例
    /// </summary>
    /// <param name="payload">负载数据</param>
    /// <param name="targetId">目标标识符</param>
    protected IpcCommandBase(TPayload payload, string? targetId = null)
    {
        CommandId = Ulid.NewUlid().ToString();
        Payload = payload;
        TargetId = targetId;
        Timestamp = DateTime.UtcNow;
    }

    /// <inheritdoc />
    public string CommandId { get; }

    /// <inheritdoc />
    public TPayload Payload { get; }

    /// <inheritdoc />
    public string? TargetId { get; }

    /// <inheritdoc />
    public DateTime Timestamp { get; }
}