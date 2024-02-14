using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
/// 可空Bool类型Json转换(用于将字符串类型的true或false转化成后端可识别的bool类型)
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
    /// <summary>
    /// Read
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override bool? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType switch
        {
            JsonTokenType.True or JsonTokenType.False => reader.GetBoolean(),
            JsonTokenType.Null                        => null,
            JsonTokenType.String                      => bool.Parse(reader.GetString()!),
            JsonTokenType.Number                      => reader.GetDouble() > 0,
            _                                         => throw new NotImplementedException($"un processed token type {reader.TokenType}")
        };

    /// <summary>
    /// Write
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, bool? value, JsonSerializerOptions options)
    {
        if (value is not null) writer.WriteBooleanValue(value.Value);
        else writer.WriteNullValue();
    }
}
