namespace EasilyNET.Ipc.Abstractions;

/// <summary>
/// IPC 命令元数据接口
/// </summary>
public interface IIpcCommandMetadata
{
    /// <summary>
    /// 命令类型名称（用于序列化和反序列化时的类型识别）
    /// </summary>
    string CommandTypeName { get; }

    /// <summary>
    /// 命令版本（用于向后兼容）
    /// </summary>
    int Version { get; }

    /// <summary>
    /// 负载数据类型
    /// </summary>
    Type PayloadType { get; }
}