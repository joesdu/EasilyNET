using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
/// 可空Bool类型Json转换(用于将字符串类型的true或false转化成后端可识别的bool类型)
/// </summary>
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
        reader.TokenType is JsonTokenType.True or JsonTokenType.False
            ? reader.GetBoolean()
            : reader.TokenType == JsonTokenType.Null
                ? null
                : reader.TokenType == JsonTokenType.String
                    ? bool.Parse(reader.GetString()!)
                    : reader.TokenType == JsonTokenType.Number
                        ? reader.GetDouble() > 0
                        : throw new NotImplementedException($"un processed token type {reader.TokenType}");

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