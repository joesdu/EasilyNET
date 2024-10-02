using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using System.Text.Json.Nodes;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.Serializers;

/// <summary>
/// JsonNode Support
/// <remarks>
///     <para>
///     This serializer handles the serialization and deserialization of <see cref="JsonNode" /> objects.
///     </para>
///     <example>
///     使用方法:
///     <code>
///  <![CDATA[
///  BsonSerializer.RegisterSerializer(new JsonNodeSerializer());
///   ]]>
///  </code>
///     </example>
/// </remarks>
/// </summary>
public sealed class JsonNodeSerializer : SerializerBase<JsonNode?>
{
    private readonly StringSerializer InnerSerializer = new();

    /// <summary>
    /// Serialize to String
    /// <remarks>
    ///     <para>
    ///     Converts the <see cref="JsonNode" /> object to its string representation for storage.
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="context">The context in which the serialization is occurring.</param>
    /// <param name="args">Additional arguments for the serialization process.</param>
    /// <param name="value">The <see cref="JsonNode" /> object to serialize.</param>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode? value)
    {
        InnerSerializer.Serialize(context, args, string.IsNullOrWhiteSpace(value?.ToString()) ? null : value.ToString());
    }

    /// <summary>
    /// Deserialize from String
    /// <remarks>
    ///     <para>
    ///     Converts the stored string representation back into a <see cref="JsonNode" /> object.
    ///     </para>
    /// </remarks>
    /// </summary>
    /// <param name="context">The context in which the deserialization is occurring.</param>
    /// <param name="args">Additional arguments for the deserialization process.</param>
    /// <returns>The deserialized <see cref="JsonNode" /> object.</returns>
    public override JsonNode? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = InnerSerializer.Deserialize(context, args);
        return string.IsNullOrWhiteSpace(value) ? null : JsonNode.Parse(value);
    }
}