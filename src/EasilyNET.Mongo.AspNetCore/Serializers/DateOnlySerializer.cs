using System.Globalization;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
/// <see cref="DateOnly" /> 序列化方式,仅存为字符串方便人类阅读
/// <remarks>
///     <para>
///     注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="DateOnlySerializerAsString" /> 和 <see cref="DateOnlySerializerAsTicks" /> 会冲突.
///     </para>
///     <example>
///     使用方法:
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new DateOnlySerializerAsString());
///   ]]>
///  </code>
///     </example>
/// </remarks>
/// </summary>
/// <param name="format"></param>
public sealed class DateOnlySerializerAsString(string format = "yyyy-MM-dd") : StructSerializerBase<DateOnly>
{
    private readonly StringSerializer InnerSerializer = new();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value) => InnerSerializer.Serialize(context, args, value.ToString(format));

    /// <inheritdoc />
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = InnerSerializer.Deserialize(context, args);
        var success = DateOnly.TryParseExact(str, format, null, DateTimeStyles.AssumeLocal, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}

/// <summary>
/// <see cref="DateOnly" /> 序列化方式,使用Ticks来记录日期
/// <remarks>
///     <para>
///     注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="DateOnlySerializerAsString" /> 和 <see cref="DateOnlySerializerAsTicks" /> 会冲突.
///     </para>
///     <example>
///     使用方法:
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new DateOnlySerializerAsTicks());
///   ]]>
///  </code>
///     </example>
/// </remarks>
/// </summary>
public sealed class DateOnlySerializerAsTicks : StructSerializerBase<DateOnly>
{
    private readonly Int64Serializer InnerSerializer = new();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value) => InnerSerializer.Serialize(context, args, value.ToDateTime(TimeOnly.MinValue).Ticks);

    /// <inheritdoc />
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = InnerSerializer.Deserialize(context, args);
        var dateTime = new DateTime(ticks, DateTimeKind.Local);
        return DateOnly.FromDateTime(dateTime);
    }
}