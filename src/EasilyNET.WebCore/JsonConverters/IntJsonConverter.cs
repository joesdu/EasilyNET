using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="int" /> and nullable <see cref="int" /> data types (used to convert string types of numbers to
///     backend-recognizable <see cref="int" /> type)
///     </para>
///     <para xml:lang="zh"><see cref="int" /> 和可空 <see cref="int" /> 数据类型的 JSON 转换器（用于将字符串类型的数字转换为后端可识别的 <see cref="int" /> 类型）</para>
/// </summary>
public sealed class IntJsonConverter : JsonConverter<int?>
{
    /// <inheritdoc />
    public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        var str = reader.GetString();
        return string.IsNullOrEmpty(str)
                   ? null
                   : int.TryParse(str, CultureInfo.CurrentCulture, out var result)
                       ? result
                       : null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteNumberValue(value.Value);
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}