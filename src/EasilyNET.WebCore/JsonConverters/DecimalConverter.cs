using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">JSON converter for decimal data type (used to convert string types of numbers to backend-recognizable decimal type)</para>
///     <para xml:lang="zh">Decimal 数据类型的 JSON 转换器（用于将字符串类型的数字转换为后端可识别的 decimal 类型）</para>
/// </summary>
public sealed class DecimalConverter : JsonConverter<decimal>
{
    /// <inheritdoc />
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Number
            ? reader.GetDecimal()
            : decimal.Parse(reader.GetString() ?? string.Empty, CultureInfo.CurrentCulture);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(CultureInfo.CurrentCulture));
}