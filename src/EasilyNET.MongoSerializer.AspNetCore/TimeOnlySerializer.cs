using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.MongoSerializer.AspNetCore;

/// <summary>
/// <see cref="TimeOnly" /> 序列化方式,仅存为字符串形式方便人类阅读
/// </summary>
/// <param name="format">格式化的格式</param>
/// <remarks>
///     <para>
///     注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="TimeOnlySerializerAsString" /> 和 <see cref="TimeOnlySerializerAsTicks" /> 会冲突.
///     </para>
/// </remarks>
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new TimeOnlySerializerAsString());
///   ]]>
///  </code>
/// </example>
public sealed class TimeOnlySerializerAsString(string format = "HH:mm:ss") : StructSerializerBase<TimeOnly>
{
    private readonly StringSerializer InnerSerializer = new();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value) => InnerSerializer.Serialize(context, args, value.ToString(format));

    /// <inheritdoc />
    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var time = InnerSerializer.Deserialize(context, args);
        var success = TimeOnly.TryParseExact(time, format, out var result);
        return success ? result : throw new("unsupported data formats.");
    }
}

/// <summary>
/// <see cref="TimeOnly" /> 序列化方式,使用Ticks来记录时间
/// </summary>
/// <remarks>
///     <para>
///     注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="TimeOnlySerializerAsString" /> 和 <see cref="TimeOnlySerializerAsTicks" /> 会冲突.
///     </para>
/// </remarks>
/// <example>
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new TimeOnlySerializerAsTicks());
///   ]]>
///  </code>
/// </example>
public sealed class TimeOnlySerializerAsTicks : StructSerializerBase<TimeOnly>
{
    private readonly Int64Serializer InnerSerializer = new();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value) => InnerSerializer.Serialize(context, args, value.Ticks);

    /// <inheritdoc />
    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = InnerSerializer.Deserialize(context, args);
        var timespan = TimeSpan.FromTicks(ticks);
        return TimeOnly.FromTimeSpan(timespan);
    }
}
