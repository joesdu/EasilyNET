using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="DateTime" /> and nullable <see cref="DateTime" /> types (used to convert string types of <see cref="DateTime" /> to
///     backend-recognizable date and time types)
///     </para>
///     <para xml:lang="zh"><see cref="DateTime" /> 和可空 <see cref="DateTime" /> 类型的 JSON 转换器（用于将字符串类型的 <see cref="DateTime" /> 转换为后端可识别的时间类型）</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers().AddJsonOptions(c => c.JsonSerializerOptions.Converters.Add(new DateTimeConverter()));
///  ]]>
///  </code>
/// </example>
public sealed class DateTimeConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (string.IsNullOrWhiteSpace(str))
        {
            return null;
        }
        return DateTime.TryParseExact(str, Constant.DateTimeFormat, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var result) ? result : null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString(Constant.DateTimeFormat, CultureInfo.CurrentCulture));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}