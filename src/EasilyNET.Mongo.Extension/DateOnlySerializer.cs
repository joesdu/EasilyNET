using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo;

/// <summary>
/// <see cref="DateOnly" /> 序列化方式,仅存为字符串方便人类阅读
/// </summary>
/// <param name="format"></param>
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new DateOnlySerializer());
///   ]]>
///  </code>
/// </example>
internal sealed class DateOnlySerializer(string format = "yyyy-MM-dd") : StructSerializerBase<DateOnly>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var str = value.ToString(format);
        context.Writer.WriteString(str);
    }

    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = context.Reader.ReadString();
        var success = DateOnly.TryParseExact(str, format, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}