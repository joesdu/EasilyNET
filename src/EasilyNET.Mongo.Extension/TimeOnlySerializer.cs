using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo;

/// <summary>
/// <see cref="TimeOnly" /> 序列化方式,仅存为字符串形式方便人类阅读
/// </summary>
/// <param name="format"></param>
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new TimeOnlySerializer());
///   ]]>
///  </code>
/// </example>
internal sealed class TimeOnlySerializer(string format = "HH:mm:ss") : StructSerializerBase<TimeOnly>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
    {
        var str = value.ToString(format);
        context.Writer.WriteString(str);
    }

    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadString();
        var success = TimeOnly.TryParseExact(ticks, format, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}