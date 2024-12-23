using System.Text.Json.Nodes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
///     <remarks>
///         <para xml:lang="en">
///         This serializer handles the serialization and deserialization of <see cref="JsonNode" /> objects.
///         </para>
///         <para xml:lang="zh">
///         该序列化器处理 <see cref="JsonNode" /> 对象的序列化和反序列化。
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new JsonNodeSerializer());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
public sealed class JsonNodeSerializer : SerializerBase<JsonNode?>
{
    private readonly StringSerializer InnerSerializer = new();

    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode? value)
    {
        InnerSerializer.Serialize(context, args, string.IsNullOrWhiteSpace(value?.ToString()) ? null : value.ToString());
    }

    /// <inheritdoc />
    public override JsonNode? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = InnerSerializer.Deserialize(context, args);
        return string.IsNullOrWhiteSpace(value) ? null : JsonNode.Parse(value);
    }
}