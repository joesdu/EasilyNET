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
/// <summary>
///     <para xml:lang="en">
///     To use this converter, register it manually in your application:
///     <code>builder.Services.Configure&lt;JsonOptions&gt;(o =&gt; o.JsonSerializerOptions.Converters.Add(new BsonDocumentJsonConverter()));</code>
///     </para>
///     <para xml:lang="zh">
///     要使用此转换器，请在应用中手动注册:
///     <code>builder.Services.Configure&lt;JsonOptions&gt;(o =&gt; o.JsonSerializerOptions.Converters.Add(new BsonDocumentJsonConverter()));</code>
///     </para>
/// </summary>
public sealed class BsonDocumentJsonConverter : JsonConverter<BsonDocument?>
{
    /// <inheritdoc />
    public override BsonDocument? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        return reader.TokenType switch
        {
            JsonTokenType.Null        => null,
            JsonTokenType.StartObject => ReadBsonDocument(ref reader),
            _                         => throw new JsonException($"Unexpected token type: {reader.TokenType} when parsing BsonDocument.")
        };
    }

    private static BsonDocument ReadBsonDocument(ref Utf8JsonReader reader)
    {
        var doc = new BsonDocument();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return doc;
            }
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }
            var name = reader.GetString()!;
            reader.Read(); // Move to value
            var value = ReadBsonValue(ref reader);
            doc.Add(name, value);
        }
        throw new JsonException("Unexpected end of JSON input while parsing BsonDocument.");
    }

    private static BsonArray ReadBsonArray(ref Utf8JsonReader reader)
    {
        var array = new BsonArray();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return array;
            }
            array.Add(ReadBsonValue(ref reader));
        }
        throw new JsonException("Unexpected end of JSON input while parsing BsonArray.");
    }

    private static BsonValue ReadBsonValue(ref Utf8JsonReader reader) =>
        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        reader.TokenType switch
        {
            JsonTokenType.StartObject => ReadBsonDocument(ref reader),
            JsonTokenType.StartArray  => ReadBsonArray(ref reader),
            JsonTokenType.String      => new BsonString(reader.GetString()),
            JsonTokenType.Number => reader.TryGetInt32(out var i)
                                        ? new BsonInt32(i)
                                        : reader.TryGetInt64(out var l)
                                            ? new BsonInt64(l)
                                            : new BsonDouble(reader.GetDouble()),
            JsonTokenType.True  => BsonBoolean.True,
            JsonTokenType.False => BsonBoolean.False,
            JsonTokenType.Null  => BsonNull.Value,
            _                   => throw new JsonException($"Unsupported token type: {reader.TokenType}")
        };

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

    private static void WriteJson(Utf8JsonWriter writer, BsonDocument bson)
    {
        writer.WriteStartObject();
        foreach (var element in bson)
        {
            WriteProperty(writer, element.Value, element.Name);
        }
        writer.WriteEndObject();
    }

    private static void WriteProperty(Utf8JsonWriter writer, BsonValue bsonValue, string? propertyName = null)
    {
        if (propertyName is not null)
        {
            writer.WritePropertyName(propertyName);
        }
        // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
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
                writer.WriteStringValue(bsonValue.ToUniversalTime());
                break;
            case BsonType.Null:
                writer.WriteNullValue();
                break;
            case BsonType.ObjectId:
                writer.WriteStringValue(bsonValue.AsObjectId.ToString());
                break;
            case BsonType.Decimal128:
                var dec128 = bsonValue.AsDecimal128;
                try
                {
                    writer.WriteNumberValue(Decimal128.ToDecimal(dec128));
                }
                catch (OverflowException)
                {
                    // Fallback to string for NaN, Infinity, or values outside decimal range
                    writer.WriteStringValue(dec128.ToString());
                }
                break;
            default:
                writer.WriteStringValue(bsonValue.ToString());
                break;
        }
    }
}