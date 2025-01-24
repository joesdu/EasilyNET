using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="bool" />  and nullable <see cref="bool" /> types (used to convert string types <see langword="true" /> or
///     <see langword="false" /> to backend-recognizable boolean types)
///     </para>
///     <para xml:lang="zh">
///     <see cref="bool" /> 和可空 <see cref="bool" /> 类型的 JSON 转换器（用于将字符串类型的 <see langword="true" /> 或 <see langword="false" /> 转换为后端可识别的
///     <see cref="bool" /> 类型）
///     </para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers().AddJsonOptions(c => c.JsonSerializerOptions.Converters.Add(new BoolConverter()));
///  ]]>
///  </code>
/// </example>
public sealed class BoolConverter : JsonConverter<bool?>
{
    /// <inheritdoc />
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            JsonTokenType.Null                        => null,
            JsonTokenType.String                      => bool.TryParse(reader.GetString(), out var result) ? result : null,
            JsonTokenType.Number                      => reader.GetDouble() > 0,
            _                                         => throw new NotImplementedException($"unprocessed token type {reader.TokenType}")
        };

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteBooleanValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}