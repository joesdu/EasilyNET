using System.Text.Json;
using System.Text.Json.Nodes;
using MongoDB.Bson;
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
/// BsonSerializer.RegisterSerializer(new JsonObjectBsonSerializer());
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
        // 将 JsonObject 转换为 BsonDocument 进行序列化
        var jsonString = value.ToJsonString();
        var bsonDocument = BsonDocument.Parse(jsonString);
        BsonDocumentSerializer.Instance.Serialize(context, bsonDocument);
    }

    /// <inheritdoc />
    public override JsonObject? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var currentBsonType = context.Reader.GetCurrentBsonType();
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (currentBsonType is BsonType.Null)
        {
            context.Reader.ReadNull();
            return null;
        }
        if (currentBsonType is BsonType.Document)
        {
            var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);
            var jsonString = bsonDocument.ToJson();
            return JsonNode.Parse(jsonString)?.AsObject();
        }
        // Handle String type - convert string to JsonObject
        if (currentBsonType is BsonType.String)
        {
            var jsonString = context.Reader.ReadString();
            try
            {
                return JsonNode.Parse(jsonString)?.AsObject();
            }
            catch (JsonException)
            {
                // If the string is not valid JSON, create a new JsonObject with the string as a property
                return new()
                {
                    ["value"] = jsonString
                };
            }
        }
        throw new BsonSerializationException($"Cannot deserialize BsonType {currentBsonType} to JsonObject.");
    }
}