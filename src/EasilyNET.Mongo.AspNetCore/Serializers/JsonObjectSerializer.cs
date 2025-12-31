using System.Text.Json.Nodes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
///     <remarks>
///         <para xml:lang="en">
///         This serializer handles the serialization and deserialization of <see cref="JsonObject" /> objects.
///         </para>
///         <para xml:lang="zh">
///         该序列化器处理 <see cref="JsonObject" /> 对象的序列化和反序列化。
///         </para>
///         <example>
///             <para xml:lang="en">Usage:</para>
///             <para xml:lang="zh">使用方法:</para>
///             <code>
/// <![CDATA[
/// BsonSerializer.RegisterSerializer(new JsonObjectSerializer());
/// ]]>
/// </code>
///         </example>
///     </remarks>
/// </summary>
public sealed class JsonObjectSerializer : SerializerBase<JsonObject?>
{
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonObject? value)
    {
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }
        JsonNodeSerializer.WriteJsonNode(context.Writer, value);
    }

    /// <inheritdoc />
    public override JsonObject? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var node = JsonNodeSerializer.ReadJsonNode(context.Reader);
        return node as JsonObject ?? (node is null ? null : new JsonObject { ["value"] = node });
    }
}