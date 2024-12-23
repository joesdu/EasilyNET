using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">JSON converter for nullable TimeOnly type (used to convert string types of time to backend-recognizable TimeOnly type)</para>
///     <para xml:lang="zh">可空 TimeOnly 类型的 JSON 转换器（用于将字符串类型的时间转换为后端可识别的 TimeOnly 类型）</para>
/// </summary>
public sealed class TimeOnlyNullJsonConverter : JsonConverter<TimeOnly?>
{
    /// <inheritdoc />
    public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        return string.IsNullOrWhiteSpace(str)
                   ? null
                   : TimeOnly.Parse(str, CultureInfo.CurrentCulture);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options) => writer.WriteStringValue(value?.ToString(Constant.TimeFormat, CultureInfo.CurrentCulture));
}