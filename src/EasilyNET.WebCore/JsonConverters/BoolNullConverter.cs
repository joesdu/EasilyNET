using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">JSON converter for nullable bool type (used to convert string types true or false to backend-recognizable bool type)</para>
///     <para xml:lang="zh">可空 Bool 类型的 JSON 转换器（用于将字符串类型的 true 或 false 转换为后端可识别的 bool 类型）</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers().AddJsonOptions(c => c.JsonSerializerOptions.Converters.Add(new BoolNullConverter()));
///  ]]>
///  </code>
/// </example>
public sealed class BoolNullConverter : JsonConverter<bool?>
{
    /// <inheritdoc />
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            JsonTokenType.Null => null,
            JsonTokenType.String => bool.Parse(reader.GetString()!),
            JsonTokenType.Number => reader.GetDouble() > 0,
            _ => throw new NotImplementedException($"un processed token type {reader.TokenType}")
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is not null) writer.WriteBooleanValue(value.Value);
        else writer.WriteNullValue();
    }
}