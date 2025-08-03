using System.Text;
using System.Text.Json;
using EasilyNET.Core.Essentials;
using MessagePack;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// IPC 命令消息（旧版本，建议使用新的泛型命令系统）
/// </summary>
/// <remarks>
/// 此类已过时，建议使用 IpcCommandBase&lt;TPayload&gt; 和相关的泛型接口。
/// 新系统提供更好的类型安全性和自动序列化支持。
/// </remarks>
[MessagePackObject]
[Obsolete("建议使用新的泛型命令系统 (IpcCommandBase<TPayload>) 以获得更好的类型安全性和自动序列化支持。此类将在未来版本中移除。")]
public sealed class IpcCommand
{
    private ReadOnlyMemory<byte> _payloadBytes;
    private string? _payloadString;

    /// <summary>
    /// 命令唯一标识符
    /// </summary>
    [Key(0)]
    public string CommandId { get; set; } = Ulid.NewUlid().ToString();

    /// <summary>
    /// 命令类型（用户自定义，例如字符串或枚举）
    /// </summary>
    [Key(1)]
    public string CommandType { get; set; } = string.Empty;

    /// <summary>
    /// 命令数据（二进制格式，支持各种序列化器）
    /// </summary>
    [Key(2)]
    public ReadOnlyMemory<byte> PayloadBytes
    {
        get => _payloadBytes;
        set
        {
            _payloadBytes = value;
            _payloadString = null; // 清除字符串缓存
        }
    }

    /// <summary>
    /// 命令数据（字符串格式，用于向后兼容）
    /// </summary>
    [Obsolete("请使用 PayloadBytes 以支持更多序列化格式，此属性将在未来版本中移除")]
    [IgnoreMember]
    public string? Payload
    {
        get
        {
            if (_payloadString == null && !_payloadBytes.IsEmpty)
            {
                _payloadString = Encoding.UTF8.GetString(_payloadBytes.Span);
            }
            return _payloadString;
        }
        set
        {
            _payloadString = value;
            _payloadBytes = string.IsNullOrEmpty(value)
                                ? ReadOnlyMemory<byte>.Empty
                                : Encoding.UTF8.GetBytes(value);
        }
    }

    /// <summary>
    /// 目标标识符（可选，例如服务或资源的 ID）
    /// </summary>
    [Key(3)]
    public string? TargetId { get; set; }

    /// <summary>
    /// 命令创建时间
    /// </summary>
    [Key(4)]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 设置字符串类型的负载数据
    /// </summary>
    /// <param name="data">字符串数据</param>
    [Obsolete("请使用 PayloadBytes 以支持更多序列化格式，此属性将在未来版本中移除")]
    public void SetPayload(string data)
    {
        Payload = data;
    }

    /// <summary>
    /// 设置二进制类型的负载数据
    /// </summary>
    /// <param name="data">二进制数据</param>
    public void SetPayload(ReadOnlyMemory<byte> data)
    {
        PayloadBytes = data;
    }

    /// <summary>
    /// 设置对象类型的负载数据（使用 JSON 序列化）
    /// </summary>
    /// <param name="data">要序列化的对象</param>
    public void SetPayload<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        Payload = json;
    }

    /// <summary>
    /// 获取字符串类型的负载数据
    /// </summary>
    /// <returns>字符串数据</returns>
    public string? GetPayloadAsString() => Payload;

    /// <summary>
    /// 获取二进制类型的负载数据
    /// </summary>
    /// <returns>二进制数据</returns>
    public ReadOnlyMemory<byte> GetPayloadAsBytes() => PayloadBytes;

    /// <summary>
    /// 获取反序列化后的对象（使用 JSON 反序列化）
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public T? GetPayloadAs<T>()
    {
        var payload = GetPayloadAsString();
        return string.IsNullOrEmpty(payload) ? default : JsonSerializer.Deserialize<T>(payload);
    }
}