using System.Text.Json.Nodes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.MongoSerializer.AspNetCore;

/// <summary>
/// JsonNode Support
/// </summary>
public sealed class JsonNodeSerializer : SerializerBase<JsonNode?>
{
    private readonly StringSerializer InnerSerializer = new();

    /// <summary>
    /// if null write {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode? value)
    {
        InnerSerializer.Serialize(context, args, string.IsNullOrWhiteSpace(value?.ToString()) ? null : value.ToString());
    }

    /// <summary>
    /// if null return {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public override JsonNode? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = InnerSerializer.Deserialize(context, args);
        return string.IsNullOrWhiteSpace(value) ? null : JsonNode.Parse(value);
    }
}