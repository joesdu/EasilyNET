using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
/// 可空Int数据类型Json转换(用于将字符串类型的数字转化成后端可识别的int类型)
/// </summary>
public sealed class IntNullConverter : JsonConverter<int?>
{
    /// <summary>
    /// Read
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="typeToConvert"></param>
    /// <param name="options"></param>
    /// <returns></returns>
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? reader.GetInt32()
            : string.IsNullOrEmpty(reader.GetString())
                ? default(int?)
                : int.Parse(reader.GetString()!);

    /// <summary>
    /// Write
    /// </summary>
    /// <param name="writer"></param>
    /// <param name="value"></param>
    /// <param name="options"></param>
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is not null) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}