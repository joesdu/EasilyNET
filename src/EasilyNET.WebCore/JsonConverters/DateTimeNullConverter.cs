using System.Text.Json;
using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.JsonConverters;

/// <summary>
/// 可空DateTime类型Json转换(用于将字符串类型的DateTime转化成后端可识别的时间类型)
/// </summary>
public class DateTimeNullConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        string.IsNullOrEmpty(reader.GetString())
            ? null
            : Convert.ToDateTime(reader.GetString());

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options) => writer.WriteStringValue(value?.ToString(Constant.DateTimeFormat));
}