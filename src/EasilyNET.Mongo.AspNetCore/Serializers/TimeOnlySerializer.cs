using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
///     <para xml:lang="en"><see cref="TimeOnly" /> serialization method, stored as a string for easy human reading</para>
///     <para xml:lang="zh"><see cref="TimeOnly" /> 序列化方式,仅存为字符串形式方便人类阅读</para>
///     <remarks>
///         <para xml:lang="en">
///         Note that only one serialization scheme of the same type is allowed globally. That is, <see cref="TimeOnlySerializerAsString" /> and
///         <see cref="TimeOnlySerializerAsTicks" /> will conflict.
///         </para>
///         <para xml:lang="zh">
///         注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="TimeOnlySerializerAsString" /> 和 <see cref="TimeOnlySerializerAsTicks" /> 会冲突.
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new TimeOnlySerializerAsString());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
/// <param name="format">
///     <para xml:lang="en">Date format</para>
///     <para xml:lang="zh">格式化的格式</para>
/// </param>
public sealed class TimeOnlySerializerAsString(string format = "HH:mm:ss.ffffff") : StructSerializerBase<TimeOnly>
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
    {
        context.Writer.WriteString(value.ToString(format, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var time = context.Reader.ReadString();
        return TimeOnly.TryParseExact(time, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                   ? result
                   : throw new BsonSerializationException($"Invalid TimeOnly format: {time}. Expected format: {format}");
    }
}

/// <summary>
///     <para xml:lang="en"><see cref="TimeOnly" /> serialization method, using Ticks to record the time</para>
///     <para xml:lang="zh"><see cref="TimeOnly" /> 序列化方式,使用Ticks来记录时间</para>
///     <remarks>
///         <para xml:lang="en">
///         Note that only one serialization scheme of the same type is allowed globally. That is, <see cref="TimeOnlySerializerAsString" /> and
///         <see cref="TimeOnlySerializerAsTicks" /> will conflict.
///         </para>
///         <para xml:lang="zh">
///         注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="TimeOnlySerializerAsString" /> 和 <see cref="TimeOnlySerializerAsTicks" /> 会冲突.
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new TimeOnlySerializerAsTicks());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
public sealed class TimeOnlySerializerAsTicks : StructSerializerBase<TimeOnly>
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, TimeOnly value)
    {
        context.Writer.WriteInt64(value.Ticks);
    }

    /// <inheritdoc />
    public override TimeOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadInt64();
        try
        {
            var timeSpan = TimeSpan.FromTicks(ticks);
            return TimeOnly.FromTimeSpan(timeSpan);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new BsonSerializationException($"Invalid TimeOnly ticks value: {ticks}", ex);
        }
    }
}