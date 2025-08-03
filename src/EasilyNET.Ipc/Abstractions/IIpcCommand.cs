namespace EasilyNET.Ipc.Abstractions;

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