using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EasilyNET.RabbitBus.AspNetCore.Abstraction;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
/// 实现 System.Text.Json 序列化器
/// </summary>
internal sealed class TextJsonSerializer : IBusSerializer
{
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null,
        IncludeFields = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type) => JsonSerializer.SerializeToUtf8Bytes(obj, type, options);

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type) => JsonSerializer.Deserialize(data, type, options);
}