using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace EasilyNET.Mongo;

/// <summary>
/// <see cref="DateOnly" /> 序列化方式,仅存为字符串方便人类阅读
/// </summary>
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new DateOnlySerializer());
///   ]]>
///  </code>
/// </example>
internal sealed class DateOnlySerializer : StructSerializerBase<DateOnly>
{
    private static string Format = "yyyy-MM-dd";

    /// <summary>
    /// 使用给默认方案: <see langword="yyyy-MM-dd" />
    /// </summary>
    public DateOnlySerializer() { }

    /// <summary>
    /// 可自定义传入 <see cref="DateOnly" /> 格式化字符串格式
    /// </summary>
    /// <param name="format">自定义 <see cref="DateOnly" /> 格式</param>
    public DateOnlySerializer(string format)
    {
        Format = format;
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        var str = value.ToString(Format);
        context.Writer.WriteString(str);
    }

    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = context.Reader.ReadString();
        var success = DateOnly.TryParseExact(str, Format, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}