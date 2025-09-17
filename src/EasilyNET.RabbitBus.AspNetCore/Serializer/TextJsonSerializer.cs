using System.Collections.Concurrent;
using System.Reflection;
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

    // 缓存泛型委托, 避免每次根据 Type 走非泛型重载的反射/转换路径
    private static readonly ConcurrentDictionary<Type, Func<object?, byte[]>> serializeCache = new();
    private static readonly ConcurrentDictionary<Type, Func<byte[], object?>> deserializeCache = new();

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type)
    {
        // null 直接序列化为 JSON null
        if (obj is null)
        {
            return JsonSerializer.SerializeToUtf8Bytes<object?>(null, options);
        }
        // 使用已缓存的泛型委托
        var del = serializeCache.GetOrAdd(type, static t => BuildSerializeDelegate(t));
        return del(obj);
    }

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type)
    {
        var del = deserializeCache.GetOrAdd(type, static t => BuildDeserializeDelegate(t));
        return del(data);
    }

    // 生成序列化委托
    private static Func<object?, byte[]> BuildSerializeDelegate(Type type)
    {
        var method = typeof(TextJsonSerializer).GetMethod(nameof(SerializeGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
                                               .MakeGenericMethod(type);
        return method.CreateDelegate<Func<object?, byte[]>>();
    }

    // 生成反序列化委托
    private static Func<byte[], object?> BuildDeserializeDelegate(Type type)
    {
        var method = typeof(TextJsonSerializer).GetMethod(nameof(DeserializeGeneric), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type);
        return method.CreateDelegate<Func<byte[], object?>>();
    }

    // 泛型静态方法供委托绑定 (JIT 后走最快路径)
    private static byte[] SerializeGeneric<T>(object? obj) => JsonSerializer.SerializeToUtf8Bytes((T?)obj, options);
    private static object? DeserializeGeneric<T>(byte[] data) => JsonSerializer.Deserialize<T>(data, options);

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
        opts.Converters.Add(new JsonStringEnumConverter());
        // 冻结配置对象 (仅 .NET 8+ 支持). 若未来目标框架低于 8 可移除此行
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