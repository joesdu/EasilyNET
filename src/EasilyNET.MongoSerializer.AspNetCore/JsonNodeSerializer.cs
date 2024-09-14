using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using System.Text.Json.Nodes;

namespace EasilyNET.MongoSerializer.AspNetCore;

/// <summary>
/// JsonNode Support
/// </summary>
public class JsonNodeSerializer : SerializerBase<JsonNode>
{
    const string EmptyObject = "{}";

    /// <summary>
    /// if null write {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <param name="value"></param>
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode value)
    {
        var json = value?.ToString() ?? EmptyObject;
        context?.Writer?.WriteString(json);
    }

    /// <summary>
    /// if null return {}
    /// </summary>
    /// <param name="context"></param>
    /// <param name="args"></param>
    /// <returns></returns>
    public override JsonNode Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        string json = context?.Reader?.ReadString() ?? EmptyObject;
        return JsonNode.Parse(json)!;
    }
}
