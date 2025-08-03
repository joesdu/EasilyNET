using System.Text;
using System.Text.Json;

namespace EasilyNET.Ipc.Models;

/// <summary>
/// IPC 命令响应
/// </summary>
public sealed class IpcCommandResponse
{
    private ReadOnlyMemory<byte> _dataBytes;
    private string? _dataString;

    /// <summary>
    /// 对应的命令 ID
    /// </summary>
    public string CommandId { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据（二进制格式，支持各种序列化器）
    /// </summary>
    public ReadOnlyMemory<byte> DataBytes
    {
        get => _dataBytes;
        set
        {
            _dataBytes = value;
            _dataString = null; // 清除字符串缓存
        }
    }

    /// <summary>
    /// 响应数据（字符串格式，用于向后兼容）
    /// </summary>
    [Obsolete("请使用 DataBytes 以支持更多序列化格式，此属性将在未来版本中移除")]
    public string? Data
    {
        get
        {
            if (_dataString == null && !_dataBytes.IsEmpty)
            {
                _dataString = Encoding.UTF8.GetString(_dataBytes.Span);
            }
            return _dataString;
        }
        set
        {
            _dataString = value;
            _dataBytes = string.IsNullOrEmpty(value)
                             ? ReadOnlyMemory<byte>.Empty
                             : Encoding.UTF8.GetBytes(value);
        }
    }

    /// <summary>
    /// 响应消息（成功或错误描述）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 操作是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 设置字符串类型的响应数据
    /// </summary>
    /// <param name="data">字符串数据</param>
    public void SetData(string data)
    {
        Data = data;
    }

    /// <summary>
    /// 设置二进制类型的响应数据
    /// </summary>
    /// <param name="data">二进制数据</param>
    public void SetData(ReadOnlyMemory<byte> data)
    {
        DataBytes = data;
    }

    /// <summary>
    /// 设置对象类型的响应数据（使用 JSON 序列化）
    /// </summary>
    /// <param name="data">要序列化的对象</param>
    public void SetData<T>(T data)
    {
        var json = JsonSerializer.Serialize(data);
        Data = json;
    }

    /// <summary>
    /// 获取字符串类型的响应数据
    /// </summary>
    /// <returns>字符串数据</returns>
    public string? GetDataAsString() => Data;

    /// <summary>
    /// 获取二进制类型的响应数据
    /// </summary>
    /// <returns>二进制数据</returns>
    public ReadOnlyMemory<byte> GetDataAsBytes() => DataBytes;

    /// <summary>
    /// 获取反序列化后的对象（使用 JSON 反序列化）
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <returns>反序列化后的对象</returns>
    public T? GetDataAs<T>()
    {
        var data = GetDataAsString();
        return string.IsNullOrEmpty(data) ? default : JsonSerializer.Deserialize<T>(data);
    }
}