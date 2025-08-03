using EasilyNET.Core.Essentials;
using MessagePack;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// 类型化IPC消息，用于在进程间传输强类型命令和数据
/// </summary>
[MessagePackObject]
public sealed class TypedIpcMessage
{
    /// <summary>
    /// 消息的唯一标识符，用于跟踪和匹配请求响应
    /// </summary>
    [Key(0)]
    public string MessageId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 命令类型的哈希值，用于标识消息负载的具体类型
    /// </summary>
    [Key(1)]
    public string TypeHash { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的负载数据，包含实际的命令或数据内容
    /// </summary>
    [Key(2)]
    public ReadOnlyMemory<byte> PayloadData { get; set; }

    /// <summary>
    /// 标识此消息是否需要响应
    /// </summary>
    [Key(3)]
    public bool RequiresResponse { get; set; }

    /// <summary>
    /// 消息创建的时间戳
    /// </summary>
    [Key(4)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 目标接收者的标识符，用于指定特定的消息接收方
    /// </summary>
    [Key(5)]
    public string? TargetId { get; set; }
}

/// <summary>
/// 类型化IPC响应消息，用于返回命令执行的结果
/// </summary>
[MessagePackObject]
public sealed class TypedIpcResponse
{
    /// <summary>
    /// 对应请求消息的唯一标识符，用于匹配请求和响应
    /// </summary>
    [Key(0)]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据类型的哈希值，用于标识响应负载的具体类型
    /// </summary>
    [Key(1)]
    public string TypeHash { get; set; } = string.Empty;

    /// <summary>
    /// 序列化的响应数据，包含实际的执行结果
    /// </summary>
    [Key(2)]
    public ReadOnlyMemory<byte> ResponseData { get; set; }

    /// <summary>
    /// 标识命令执行是否成功
    /// </summary>
    [Key(3)]
    public bool Success { get; set; }

    /// <summary>
    /// 当执行失败时的错误信息
    /// </summary>
    [Key(4)]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 响应生成的时间戳
    /// </summary>
    [Key(5)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// 基础类型化命令接口，定义所有命令必须具备的基本属性
/// </summary>
public interface ITypedCommand
{
    /// <summary>
    /// 命令的唯一标识符，用于跟踪和识别命令实例
    /// </summary>
    string CommandId { get; }

    /// <summary>
    /// 命令创建的时间戳
    /// </summary>
    DateTime Timestamp { get; }
}

/// <summary>
/// 带有响应类型的类型化命令接口，用于需要返回特定类型响应的命令
/// </summary>
/// <typeparam name="TResponse">响应数据的类型</typeparam>
public interface ITypedCommand<TResponse> : ITypedCommand { }

/// <summary>
/// 类型化命令的基础抽象类，提供命令标识符和时间戳的默认实现
/// </summary>
public abstract class TypedCommandBase : ITypedCommand
{
    /// <summary>
    /// 命令的唯一标识符，自动生成ULID格式的字符串
    /// </summary>
    public string CommandId { get; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 命令创建的UTC时间戳
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

/// <summary>
/// 带有特定响应类型的类型化命令抽象类
/// </summary>
/// <typeparam name="TResponse">此命令预期返回的响应数据类型</typeparam>
public abstract class TypedCommand<TResponse> : TypedCommandBase, ITypedCommand<TResponse> { }