using EasilyNET.Core.Essentials;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// 类型化IPC消息，用于在进程间传输强类型命令和数据
/// </summary>
public sealed class IpcMessage<TPayload>
{
    /// <summary>
    /// 消息的唯一标识符，用于跟踪和匹配请求响应
    /// </summary>
    public string MessageId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 命令类型的哈希值，用于标识消息负载的具体类型
    /// </summary>
    public string TypeHash { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的负载数据，包含实际的命令或数据内容
    /// </summary>
    public TPayload? Payload { get; set; }

    /// <summary>
    /// 标识此消息是否需要响应
    /// </summary>
    public bool RequiresResponse { get; set; }

    /// <summary>
    /// 消息创建的时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 目标接收者的标识符，用于指定特定的消息接收方
    /// </summary>
    public string? TargetId { get; set; }
}

/// <summary>
/// 类型化IPC响应消息，用于返回命令执行的结果
/// </summary>
public sealed class IpcResponse
{
    /// <summary>
    /// 对应请求消息的唯一标识符，用于匹配请求和响应
    /// </summary>
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据类型的哈希值，用于标识响应负载的具体类型
    /// </summary>
    public string TypeHash { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的响应数据，包含实际的执行结果
    /// </summary>
    public ReadOnlyMemory<byte> ResponseData { get; set; }

    /// <summary>
    /// 标识命令执行是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 当执行失败时的错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 响应生成的时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 创建一个表示成功的响应
    /// </summary>
    /// <param name="data">响应数据</param>
    /// <param name="messageId">关联的消息ID</param>
    /// <returns>一个成功的 IpcResponse 实例</returns>
    public static IpcResponse CreateSuccess(object? data, string? messageId = null)
    {
        return new()
        {
            Success = true,
            ResponseData = data is null ? ReadOnlyMemory<byte>.Empty : System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(data),
            MessageId = messageId ?? string.Empty,
            TypeHash = data?.GetType().FullName?.GetHashCode().ToString("X") ?? string.Empty
        };
    }

    /// <summary>
    /// 创建一个表示失败的响应
    /// </summary>
    /// <param name="errorMessage">错误信息</param>
    /// <param name="messageId">关联的消息ID</param>
    /// <returns>一个失败的 IpcResponse 实例</returns>
    public static IpcResponse CreateError(string errorMessage, string? messageId = null)
    {
        return new()
        {
            Success = false,
            ErrorMessage = errorMessage,
            MessageId = messageId ?? string.Empty
        };
    }
}