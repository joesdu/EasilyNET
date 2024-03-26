using EasilyNET.RabbitBus.AspNetCore.Abstraction;
using System.Text.Json;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
/// 实现 System.Text.Json 序列化器
/// </summary>
internal sealed class TextJsonSerializer : IBusSerializer
{
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type) => JsonSerializer.SerializeToUtf8Bytes(obj, type, options);

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type) => JsonSerializer.Deserialize(data, type, options);
}