using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;

// ReSharper disable UnusedType.Global

namespace EasilyNET.Mongo.AspNetCore.JsonConverters;

/// <summary>
///     <para xml:lang="en">
///     JSON converter for <see cref="BsonDocument" /> and nullable <see cref="BsonDocument" /> types (used to convert JSON to backend-recognizable
///     <see cref="BsonDocument" /> type)
///     </para>
///     <para xml:lang="zh"><see cref="BsonDocument" /> 和可空 <see cref="BsonDocument" /> 类型的 JSON 转换器（用于将 JSON 转换为后端可识别的 <see cref="BsonDocument" /> 类型）</para>
/// </summary>
/// <example>
///     <code>
/// <![CDATA[
///  builder.Services.AddControllers().AddJsonOptions(c => c.JsonSerializerOptions.Converters.Add(new BsonDocumentConverter()));
///  ]]>
///  </code>
/// </example>
public sealed class BsonDocumentJsonConverter : JsonConverter<BsonDocument?>
{
    /// <inheritdoc />
    public override BsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var jsonDoc = JsonDocument.ParseValue(ref reader);
        var succeed = BsonDocument.TryParse(jsonDoc.RootElement.GetRawText(), out var result);
        return succeed ? result : null;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, BsonDocument? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteJson(writer, value);
        }
    }

    private void WriteJson(Utf8JsonWriter writer, BsonDocument bson)
    {
        writer.WriteStartObject();
        foreach (var element in bson)
        {
            WriteProperty(writer, element.Value, element.Name);
        }
        writer.WriteEndObject();
    }

    private void WriteProperty(Utf8JsonWriter writer, BsonValue bsonValue, string? propertyName = null)
    {
        if (propertyName != null)
        {
            writer.WritePropertyName(propertyName);
        }
        switch (bsonValue.BsonType)
        {
            case BsonType.Int32:
                writer.WriteNumberValue(bsonValue.AsInt32);
                break;
            case BsonType.Int64:
                writer.WriteNumberValue(bsonValue.AsInt64);
                break;
            case BsonType.Double:
                writer.WriteNumberValue(bsonValue.AsDouble);
                break;
            case BsonType.String:
                writer.WriteStringValue(bsonValue.AsString);
                break;
            case BsonType.Document:
                WriteJson(writer, bsonValue.AsBsonDocument);
                break;
            case BsonType.Array:
                writer.WriteStartArray();
                foreach (var item in bsonValue.AsBsonArray)
                {
                    WriteProperty(writer, item);
                }
                writer.WriteEndArray();
                break;
            case BsonType.Boolean:
                writer.WriteBooleanValue(bsonValue.AsBoolean);
                break;
            case BsonType.DateTime:
                writer.WriteStringValue(bsonValue.ToLocalTime());
                break;
            case BsonType.Null:
                writer.WriteNullValue();
                break;
            case BsonType.ObjectId:
                writer.WriteStringValue(bsonValue.AsObjectId.ToString());
                break;
            case BsonType.EndOfDocument:
            case BsonType.Binary:
            case BsonType.Undefined:
            case BsonType.RegularExpression:
            case BsonType.JavaScript:
            case BsonType.Symbol:
            case BsonType.JavaScriptWithScope:
            case BsonType.Timestamp:
            case BsonType.Decimal128:
            case BsonType.MinKey:
            case BsonType.MaxKey:
            default:
                writer.WriteStringValue(bsonValue.ToString());
                break;
        }
    }
}