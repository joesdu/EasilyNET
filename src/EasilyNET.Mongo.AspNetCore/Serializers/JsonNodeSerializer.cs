using System.Text.Json.Nodes;
using MongoDB.Bson;
using MongoDB.Bson.IO;
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
        if (value is null)
        {
            context.Writer.WriteNull();
            return;
        }
        WriteJsonNode(context.Writer, value);
    }

    /// <inheritdoc />
    public override JsonNode? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) => ReadJsonNode(context.Reader);

    internal static void WriteJsonNode(IBsonWriter writer, JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                writer.WriteStartDocument();
                foreach (var kvp in obj)
                {
                    writer.WriteName(kvp.Key);
                    if (kvp.Value is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        WriteJsonNode(writer, kvp.Value);
                    }
                }
                writer.WriteEndDocument();
                break;
            case JsonArray arr:
                writer.WriteStartArray();
                foreach (var item in arr)
                {
                    if (item is null)
                    {
                        writer.WriteNull();
                    }
                    else
                    {
                        WriteJsonNode(writer, item);
                    }
                }
                writer.WriteEndArray();
                break;
            case JsonValue val:
                WriteJsonValue(writer, val);
                break;
            default:
                throw new BsonSerializationException($"Unsupported JsonNode type: {node.GetType()}");
        }
    }

    private static void WriteJsonValue(IBsonWriter writer, JsonValue val)
    {
        if (val.TryGetValue(out int i))
        {
            writer.WriteInt32(i);
        }
        else if (val.TryGetValue(out long l))
        {
            writer.WriteInt64(l);
        }
        else if (val.TryGetValue(out double d))
        {
            writer.WriteDouble(d);
        }
        else if (val.TryGetValue(out bool b))
        {
            writer.WriteBoolean(b);
        }
        else if (val.TryGetValue(out string? s))
        {
            writer.WriteString(s);
        }
        else if (val.TryGetValue(out decimal dec))
        {
            writer.WriteDecimal128(dec);
        }
        else if (val.TryGetValue(out DateTime dt))
        {
            // Store DateTime as BSON DateTime (milliseconds since Unix epoch, UTC)
            writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(dt.ToUniversalTime()));
        }
        else if (val.TryGetValue(out DateTimeOffset dto))
        {
            // Store DateTimeOffset as BSON DateTime using its UTC instant
            writer.WriteDateTime(BsonUtils.ToMillisecondsSinceEpoch(dto.UtcDateTime));
        }
        else if (val.TryGetValue(out Guid g))
        {
            writer.WriteString(g.ToString());
        }
        else
        {
            writer.WriteString(val.ToString());
        }
    }

    internal static JsonNode? ReadJsonNode(IBsonReader reader)
    {
        var currentBsonType = reader.GetCurrentBsonType();
        switch (currentBsonType)
        {
            case BsonType.Null:
                reader.ReadNull();
                return null;
            case BsonType.Document:
                reader.ReadStartDocument();
                var obj = new JsonObject();
                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    var name = reader.ReadName();
                    obj[name] = ReadJsonNode(reader);
                }
                reader.ReadEndDocument();
                return obj;
            case BsonType.Array:
                reader.ReadStartArray();
                var arr = new JsonArray();
                while (reader.ReadBsonType() != BsonType.EndOfDocument)
                {
                    arr.Add(ReadJsonNode(reader));
                }
                reader.ReadEndArray();
                return arr;
            case BsonType.String:
                return JsonValue.Create(reader.ReadString());
            case BsonType.Boolean:
                return JsonValue.Create(reader.ReadBoolean());
            case BsonType.Int32:
                return JsonValue.Create(reader.ReadInt32());
            case BsonType.Int64:
                return JsonValue.Create(reader.ReadInt64());
            case BsonType.Double:
                return JsonValue.Create(reader.ReadDouble());
            case BsonType.Decimal128:
                return JsonValue.Create((decimal)reader.ReadDecimal128());
            case BsonType.DateTime:
                return JsonValue.Create(reader.ReadDateTime().ToString("o"));
            case BsonType.ObjectId:
                return JsonValue.Create(reader.ReadObjectId().ToString());
            case BsonType.Binary:
                return JsonValue.Create(Convert.ToBase64String(reader.ReadBinaryData().Bytes));
            case BsonType.Timestamp:
                var rawTimestamp = reader.ReadTimestamp();
                var bsonTimestamp = new BsonTimestamp(rawTimestamp);
                var timestampDateTime = BsonUtils.ToDateTimeFromMillisecondsSinceEpoch(bsonTimestamp.Timestamp);
                return JsonValue.Create(timestampDateTime.ToString("o"));
            case BsonType.RegularExpression:
                return JsonValue.Create(reader.ReadRegularExpression().Pattern);
            case BsonType.JavaScript:
                return JsonValue.Create(reader.ReadJavaScript());
            case BsonType.Symbol:
                return JsonValue.Create(reader.ReadSymbol());
            case BsonType.JavaScriptWithScope:
                return JsonValue.Create(reader.ReadJavaScriptWithScope());
            case BsonType.MinKey:
                reader.ReadMinKey();
                return JsonValue.Create("MinKey");
            case BsonType.MaxKey:
                reader.ReadMaxKey();
                return JsonValue.Create("MaxKey");
            case BsonType.Undefined:
                reader.ReadUndefined();
                return null;
            case BsonType.EndOfDocument:
            default:
                throw new BsonSerializationException($"Cannot deserialize BsonType {currentBsonType} to JsonNode.");
        }
    }
}