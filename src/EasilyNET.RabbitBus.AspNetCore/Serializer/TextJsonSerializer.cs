using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EasilyNET.RabbitBus.Core.Abstraction;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
///     <para xml:lang="en">Implements System.Text.Json serializer</para>
///     <para xml:lang="zh">实现 System.Text.Json 序列化器</para>
/// </summary>
internal sealed class TextJsonSerializer : IBusSerializer
{
    // 静态只读配置一次, 并在 .NET 8+ 上调用 MakeReadOnly() 以获得更好的性能
    private static readonly JsonSerializerOptions options = CreateOptions();

    // 缓存 JsonTypeInfo, 避免每次 GetTypeInfo 的开销
    // 使用非泛型 JsonTypeInfo 基类，避免协变转换失败
    private static readonly ConcurrentDictionary<Type, JsonTypeInfo> typeInfoCache = new();

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type)
    {
        if (obj is null)
        {
            return JsonSerializer.SerializeToUtf8Bytes<object?>(null, options);
        }
        var typeInfo = GetTypeInfo(type);
        return JsonSerializer.SerializeToUtf8Bytes(obj, typeInfo);
    }

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type)
    {
        var typeInfo = GetTypeInfo(type);
        return JsonSerializer.Deserialize(data, typeInfo);
    }

    private static JsonTypeInfo GetTypeInfo(Type type)
    {
        return typeInfoCache.GetOrAdd(type, static t => options.GetTypeInfo(t));
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var opts = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNameCaseInsensitive = true,
            IncludeFields = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };
        try
        {
            opts.MakeReadOnly();
        }
        catch
        {
            /* ignore if not supported */
        }
        return opts;
    }
}