using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">JSON converter for nullable int data type (used to convert string types of numbers to backend-recognizable int type)</para>
///     <para xml:lang="zh">可空 Int 数据类型的 JSON 转换器（用于将字符串类型的数字转换为后端可识别的 int 类型）</para>
/// </summary>
public sealed class IntNullConverter : JsonConverter<int?>
{
    /// <inheritdoc />
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        var str = reader.GetString();
        return string.IsNullOrEmpty(str) ? null : int.Parse(str, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value is not null) writer.WriteNumberValue(value.Value);
        else writer.WriteNullValue();
    }
}