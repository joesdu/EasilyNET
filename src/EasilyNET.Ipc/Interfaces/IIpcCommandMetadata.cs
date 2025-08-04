namespace EasilyNET.Ipc.Interfaces;

/// <summary>
/// IPC 命令元数据接口，用于描述命令的类型信息
/// </summary>
public interface IIpcCommandMetadata
{
    /// <summary>
    /// 命令类型
    /// </summary>
    Type CommandType { get; }

    /// <summary>
    /// 命令名称
    /// </summary>
    string CommandName { get; }

    /// <summary>
    /// 负载数据类型
    /// </summary>
    Type PayloadType { get; }

    /// <summary>
    /// 响应数据类型（如果有）
    /// </summary>
    Type? ResponseType { get; }

    /// <summary>
    /// 类型哈希值，用于快速查找和匹配
    /// </summary>
    string TypeHash { get; }
}

/// <summary>
/// IPC 命令元数据实现
/// </summary>
/// <remarks>
/// 初始化命令元数据
/// </remarks>
/// <param name="commandType">命令类型</param>
/// <param name="commandName">命令名称</param>
/// <param name="payloadType">负载数据类型</param>
/// <param name="responseType">响应数据类型</param>
/// <param name="typeHash">类型哈希值</param>
public sealed class IpcCommandMetadata(Type commandType, string commandName, Type payloadType, Type? responseType, string typeHash) : IIpcCommandMetadata
{
    /// <inheritdoc />
    public Type CommandType { get; } = commandType;

    /// <inheritdoc />
    public string CommandName { get; } = commandName;

    /// <inheritdoc />
    public Type PayloadType { get; } = payloadType;

    /// <inheritdoc />
    public Type? ResponseType { get; } = responseType;

    /// <inheritdoc />
    public string TypeHash { get; } = typeHash;
}