namespace EasilyNET.Ipc.Models;

/// <summary>
/// 泛型 IPC 命令响应
/// </summary>
/// <typeparam name="TData">响应数据类型</typeparam>
/// <param name="commandId">对应的命令 ID</param>
/// <param name="success">操作是否成功</param>
/// <param name="data">响应数据</param>
/// <param name="message">响应消息</param>
public sealed class IpcCommandResponse<TData>(string commandId, bool success, TData? data = default, string? message = null)
{
    /// <summary>
    /// 对应的命令 ID
    /// </summary>
    public string CommandId { get; } = commandId;

    /// <summary>
    /// 响应数据
    /// </summary>
    public TData? Data { get; } = data;

    /// <summary>
    /// 响应消息（成功或错误描述）
    /// </summary>
    public string? Message { get; } = message;

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; } = success;

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// 创建成功响应
    /// </summary>
    /// <param name="commandId">命令 ID</param>
    /// <param name="data">响应数据</param>
    /// <param name="message">成功消息</param>
    /// <returns>成功响应实例</returns>
    public static IpcCommandResponse<TData> CreateSuccess(string commandId, TData? data = default, string? message = null) => new(commandId, true, data, message);

    /// <summary>
    /// 创建失败响应
    /// </summary>
    /// <param name="commandId">命令 ID</param>
    /// <param name="message">错误消息</param>
    /// <returns>失败响应实例</returns>
    public static IpcCommandResponse<TData> CreateFailure(string commandId, string message) => new(commandId, false, default, message);
}