using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for nullable DateTime type (used to convert string types of DateTime to backend-recognizable date and time
///     types)
///     </para>
///     <para xml:lang="zh">可空 DateTime 类型的 JSON 转换器（用于将字符串类型的 DateTime 转换为后端可识别的时间类型）</para>
/// </summary>
public sealed class DateTimeNullConverter : JsonConverter<DateTime?>
{
    /// <inheritdoc />
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        string.IsNullOrEmpty(reader.GetString())
            ? null
            : Convert.ToDateTime(reader.GetString(), CultureInfo.CurrentCulture);

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) => writer.WriteStringValue(value?.ToString(Constant.DateTimeFormat, CultureInfo.CurrentCulture));
}