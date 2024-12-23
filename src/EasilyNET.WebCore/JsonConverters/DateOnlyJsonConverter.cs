using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="DateOnly" /> type (used to convert string types of dates to backend-recognizable
///     <see cref="DateOnly" /> type)
///     </para>
///     <para xml:lang="zh"><see cref="DateOnly" /> 类型的 JSON 转换器（用于将字符串类型的日期转换为后端可识别的 <see cref="DateOnly" /> 类型）</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers().AddJsonOptions(c => c.JsonSerializerOptions.Converters.Add(new DateOnlyJsonConverter()));
///  ]]>
///  </code>
/// </example>
public sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    /// <inheritdoc />
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        ArgumentException.ThrowIfNullOrWhiteSpace(str);
        return DateOnly.ParseExact(str, Constant.DateFormat, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToString(Constant.DateFormat, CultureInfo.CurrentCulture));
}