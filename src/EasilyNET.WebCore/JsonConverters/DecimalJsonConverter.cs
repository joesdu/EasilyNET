using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="decimal" /> and nullable <see cref="decimal" /> data types (used to convert string types of
///     numbers to backend-recognizable <see cref="decimal" /> type)
///     </para>
///     <para xml:lang="zh"><see cref="decimal" /> 和可空 <see cref="decimal" /> 数据类型的 JSON 转换器（用于将字符串类型的数字转换为后端可识别的 <see cref="decimal" /> 类型）</para>
/// </summary>
public sealed class DecimalJsonConverter : JsonConverter<decimal?>
{
    /// <inheritdoc />
    public override decimal? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetDecimal();
        }
        var str = reader.GetString();
        return string.IsNullOrWhiteSpace(str)
                   ? null
                   : decimal.TryParse(str, CultureInfo.CurrentCulture, out var result)
                       ? result
                       : null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(CultureInfo.CurrentCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}