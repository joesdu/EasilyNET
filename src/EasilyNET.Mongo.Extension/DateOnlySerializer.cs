using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo;

/// <summary>
/// DateOnly序列化方式
/// </summary>
internal sealed class DateOnlySerializer : StructSerializerBase<DateOnly>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var str = value.ToString("yyyy-MM-dd");
        context.Writer.WriteString(str);
    }

    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = context.Reader.ReadString();
        var success = DateOnly.TryParse(str, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}