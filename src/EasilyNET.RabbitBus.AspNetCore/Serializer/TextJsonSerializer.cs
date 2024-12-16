using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using EasilyNET.RabbitBus.Core.Abstraction;

namespace EasilyNET.RabbitBus.AspNetCore.Serializer;

/// <summary>
/// 实现 System.Text.Json 序列化器
/// </summary>
internal sealed class TextJsonSerializer : IBusSerializer
{
    private static readonly JsonSerializerOptions options = new()
    {
        WriteIndented = false,                                               // 不缩进，生成紧凑的JSON
        PropertyNameCaseInsensitive = true,                                  // 属性名称不区分大小写
        IncludeFields = false,                                               // 不包含字段，只序列化属性
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,                   // 属性命名策略为驼峰命名
        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,                    // 字典键命名策略为驼峰命名
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),                // 使用默认的类型信息解析器
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,        // 忽略值为null的属性
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals, // 允许命名的浮点数文字
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,               // 使用不安全的放松JSON转义
        Converters = { new JsonStringEnumConverter() }                       // 添加枚举转换器，枚举值序列化为原始名称字符串
    };

    /// <inheritdoc />
    public byte[] Serialize(object? obj, Type type) => JsonSerializer.SerializeToUtf8Bytes(obj, type, options);

    /// <inheritdoc />
    public object? Deserialize(byte[] data, Type type) => JsonSerializer.Deserialize(data, type, options);
}