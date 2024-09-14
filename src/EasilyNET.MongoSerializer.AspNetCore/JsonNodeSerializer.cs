using System.Text.Json.Nodes;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

// ReSharper disable UnusedType.Global

namespace EasilyNET.MongoSerializer.AspNetCore;

/// <summary>
/// JsonNode Support
/// </summary>
public sealed class JsonNodeSerializer : SerializerBase<JsonNode>
{
    private const string EmptyObject = "{}";

    /// <summary>
    /// if null write {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode? value)
    {
        var json = value?.ToString() ?? EmptyObject;
        context.Writer.WriteString(json);
    }

    /// <summary>
    /// if null return {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public override JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var value = context.Reader.ReadString();
        var json = string.IsNullOrWhiteSpace(value) ? EmptyObject : value;
        return JsonNode.Parse(json)!;
    }
}