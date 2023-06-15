using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo;

/// <summary>
/// <see cref="TimeOnly" /> 序列化方式,仅存为字符串形式方便人类阅读
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new TimeOnlySerializer());
///   ]]>
///  </code>
/// </example>
/// </summary>
internal sealed class TimeOnlySerializer : StructSerializerBase<TimeOnly>
{
    private static string Format = "HH:mm:ss";

    /// <summary>
    /// 使用给默认方案: <see langword="HH:mm:ss" />
    /// </summary>
    public TimeOnlySerializer() { }

    /// <summary>
    /// 可自定义传入 <see cref="TimeOnly" /> 格式化字符串格式
    /// </summary>
    /// <param name="format"></param>
    public TimeOnlySerializer(string format)
    {
        Format = format;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
    {
        var str = value.ToString(Format);
        context.Writer.WriteString(str);
    }

    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadString();
        var success = TimeOnly.TryParseExact(ticks, Format, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}