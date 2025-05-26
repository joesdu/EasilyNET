using System.Text.Json;
using System.Text.Json.Nodes;
using EasilyNET.Mongo.AspNetCore.Converter;
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
        // 更安全地将 JsonObject 转换为 BsonDocument
        var jsonString = value.ToJsonString();
        BsonDocument bsonDocument;
        try
        {
            bsonDocument = BsonDocument.Parse(jsonString);
        }
        catch (Exception ex)
        {
            throw new BsonSerializationException($"Failed to parse JsonObject to BsonDocument. JSON: {jsonString}", ex);
        }
        BsonDocumentSerializer.Instance.Serialize(context, bsonDocument);
    }

    /// <inheritdoc />
    public override JsonObject? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var currentBsonType = context.Reader.GetCurrentBsonType();
        switch (currentBsonType)
        {
            case BsonType.Null:
                context.Reader.ReadNull();
                return null;
            case BsonType.Document:
            {
                var bsonDocument = BsonDocumentSerializer.Instance.Deserialize(context, args);
                // 尝试递归转换，保留类型信息
                return BsonJsonNodeConverter.BsonToJsonObject(bsonDocument);
            }
            case BsonType.String:
            {
                var jsonString = context.Reader.ReadString();
                try
                {
                    return JsonNode.Parse(jsonString)?.AsObject();
                }
                catch (JsonException)
                {
                    return new() { ["value"] = jsonString };
                }
            }
            case BsonType.Array:
            {
                var bsonArray = BsonArraySerializer.Instance.Deserialize(context, args);
                return new() { ["array"] = BsonJsonNodeConverter.BsonToJsonNode(bsonArray) };
            }
            case BsonType.Boolean:
                return new() { ["value"] = context.Reader.ReadBoolean() };
            case BsonType.DateTime:
                return new() { ["value"] = context.Reader.ReadDateTime().ToString() };
            case BsonType.Double:
                return new() { ["value"] = context.Reader.ReadDouble() };
            case BsonType.Int32:
                return new() { ["value"] = context.Reader.ReadInt32() };
            case BsonType.Int64:
                return new() { ["value"] = context.Reader.ReadInt64() };
            case BsonType.Decimal128:
                return new() { ["value"] = (decimal)context.Reader.ReadDecimal128() };
            case BsonType.ObjectId:
                return new() { ["value"] = context.Reader.ReadObjectId().ToString() };
            case BsonType.Binary:
                return new() { ["value"] = Convert.ToBase64String(context.Reader.ReadBinaryData().Bytes) };
            case BsonType.RegularExpression:
                return new() { ["value"] = context.Reader.ReadRegularExpression().Pattern };
            case BsonType.JavaScript:
                return new() { ["value"] = context.Reader.ReadJavaScript() };
            case BsonType.Symbol:
                return new() { ["value"] = context.Reader.ReadSymbol() };
            case BsonType.JavaScriptWithScope:
                return new() { ["value"] = context.Reader.ReadJavaScriptWithScope() };
            case BsonType.Timestamp:
                return new() { ["value"] = context.Reader.ReadTimestamp().ToString() };
            case BsonType.MinKey:
                context.Reader.ReadMinKey();
                return new() { ["value"] = "MinKey" };
            case BsonType.MaxKey:
                context.Reader.ReadMaxKey();
                return new() { ["value"] = "MaxKey" };
            case BsonType.Undefined:
                context.Reader.ReadUndefined();
                return null;
            case BsonType.EndOfDocument:
            default:
                throw new BsonSerializationException($"Cannot deserialize BsonType {currentBsonType} to JsonObject.");
        }
    }
}