using System.Globalization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
///     <para xml:lang="en"><see cref="DateOnly" /> serialization method, stored as a string for easy human reading</para>
///     <para xml:lang="zh"><see cref="DateOnly" /> 序列化方式,仅存为字符串方便人类阅读</para>
///     <remarks>
///         <para xml:lang="en">
///         Note that only one serialization scheme of the same type is allowed globally. That is, <see cref="DateOnlySerializerAsString" /> and
///         <see cref="DateOnlySerializerAsTicks" /> will conflict.
///         </para>
///         <para xml:lang="zh">
///         注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="DateOnlySerializerAsString" /> 和 <see cref="DateOnlySerializerAsTicks" /> 会冲突.
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new DateOnlySerializerAsString());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
/// <param name="format">
///     <para xml:lang="en">Date format</para>
///     <para xml:lang="zh">日期格式</para>
/// </param>
public sealed class DateOnlySerializerAsString(string format = "yyyy-MM-dd") : StructSerializerBase<DateOnly>
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        context.Writer.WriteString(value.ToString(format, CultureInfo.InvariantCulture));
    }

    /// <inheritdoc />
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var str = context.Reader.ReadString();
        return DateOnly.TryParseExact(str, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
                   ? result
                   : throw new BsonSerializationException($"Invalid DateOnly format: {str}. Expected format: {format}");
    }
}

/// <summary>
///     <para xml:lang="en"><see cref="DateOnly" /> serialization method, using Ticks to record the date</para>
///     <para xml:lang="zh"><see cref="DateOnly" /> 序列化方式,使用Ticks来记录日期</para>
///     <remarks>
///         <para xml:lang="en">
///         Note that only one serialization scheme of the same type is allowed globally. That is, <see cref="DateOnlySerializerAsString" /> and
///         <see cref="DateOnlySerializerAsTicks" /> will conflict.
///         </para>
///         <para xml:lang="zh">
///         注意同一类型的序列化方案全局只允许注册一种.也就是说 <see cref="DateOnlySerializerAsString" /> 和 <see cref="DateOnlySerializerAsTicks" /> 会冲突.
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new DateOnlySerializerAsTicks());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
public sealed class DateOnlySerializerAsTicks : StructSerializerBase<DateOnly>
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, DateOnly value)
    {
        context.Writer.WriteInt64(value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Ticks);
    }

    /// <inheritdoc />
    public override DateOnly Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var ticks = context.Reader.ReadInt64();
        return DateOnly.FromDateTime(new(ticks, DateTimeKind.Utc));
    }
}