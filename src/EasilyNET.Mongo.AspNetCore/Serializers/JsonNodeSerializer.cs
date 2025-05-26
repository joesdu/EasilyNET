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
    /// <inheritdoc />
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, JsonNode? value)
    {
        switch (value)
        {
            case null:
                context.Writer.WriteNull();
                return;
            case JsonValue jsonValue:
                // Handle scalar values directly
                switch (jsonValue.GetValue<object>())
                {
                    case string strValue:
                        context.Writer.WriteString(strValue);
                        break;
                    case int intValue:
                        context.Writer.WriteInt32(intValue);
                        break;
                    case long longValue:
                        context.Writer.WriteInt64(longValue);
                        break;
                    case double doubleValue:
                        context.Writer.WriteDouble(doubleValue);
                        break;
                    case bool boolValue:
                        context.Writer.WriteBoolean(boolValue);
                        break;
                    case decimal decimalValue:
                        context.Writer.WriteDecimal128(decimalValue);
                        break;
                    default:
                        throw new BsonSerializationException($"Unsupported scalar value type: {jsonValue.GetValue<object?>()?.GetType()}");
                }
                return;
        }
        var jsonString = value.ToJsonString();
        var bsonDocument = BsonDocument.Parse(jsonString);
        BsonDocumentSerializer.Instance.Serialize(context, bsonDocument);
    }

    /// <inheritdoc />
    public override JsonNode? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
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
                return BsonJsonNodeConverter.BsonToJsonNode(bsonDocument);
            }
            case BsonType.String:
            {
                var jsonString = context.Reader.ReadString();
                try
                {
                    return JsonNode.Parse(jsonString);
                }
                catch (JsonException)
                {
                    return new JsonObject { ["value"] = jsonString };
                }
            }
            case BsonType.Array:
            {
                var bsonArray = BsonArraySerializer.Instance.Deserialize(context, args);
                return BsonJsonNodeConverter.BsonToJsonNode(bsonArray);
            }
            case BsonType.Boolean:
                return context.Reader.ReadBoolean();
            case BsonType.DateTime:
                return context.Reader.ReadDateTime().ToString("o");
            case BsonType.Double:
                return context.Reader.ReadDouble();
            case BsonType.Int32:
                return context.Reader.ReadInt32();
            case BsonType.Int64:
                return context.Reader.ReadInt64();
            case BsonType.Decimal128:
                return (decimal)context.Reader.ReadDecimal128();
            case BsonType.ObjectId:
                return context.Reader.ReadObjectId().ToString();
            case BsonType.Binary:
                return Convert.ToBase64String(context.Reader.ReadBinaryData().Bytes);
            case BsonType.RegularExpression:
                return context.Reader.ReadRegularExpression().Pattern;
            case BsonType.JavaScript:
                return context.Reader.ReadJavaScript();
            case BsonType.Symbol:
                return context.Reader.ReadSymbol();
            case BsonType.JavaScriptWithScope:
                return context.Reader.ReadJavaScriptWithScope();
            case BsonType.Timestamp:
            {
                var bsonTimestamp = context.Reader.ReadTimestamp();
                var timestampDateTime = DateTimeOffset.FromUnixTimeSeconds(bsonTimestamp).UtcDateTime;
                return timestampDateTime.ToString("o"); // ISO 8601 format
            }
            case BsonType.MinKey:
                context.Reader.ReadMinKey();
                return "MinKey";
            case BsonType.MaxKey:
                context.Reader.ReadMaxKey();
                return "MaxKey";
            case BsonType.Undefined:
                context.Reader.ReadUndefined();
                return null;
            case BsonType.EndOfDocument:
            default:
                throw new BsonSerializationException($"Cannot deserialize BsonType {currentBsonType} to JsonNode.");
        }
    }
}