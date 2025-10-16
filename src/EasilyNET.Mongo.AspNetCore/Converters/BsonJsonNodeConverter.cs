using System.Text.Json.Nodes;
using MongoDB.Bson;

namespace EasilyNET.Mongo.AspNetCore.Converters;

internal static class BsonJsonNodeConverter
{
    internal static JsonNode? BsonToJsonNode(BsonValue value)
    {
        return value.BsonType switch
        {
            BsonType.Document            => BsonToJsonObject(value.AsBsonDocument),
            BsonType.Array               => new JsonArray([.. value.AsBsonArray.Select(BsonToJsonNode)]),
            BsonType.Boolean             => value.AsBoolean,
            BsonType.DateTime            => value.ToUniversalTime().ToString("o"),
            BsonType.Double              => value.AsDouble,
            BsonType.Int32               => value.AsInt32,
            BsonType.Int64               => value.AsInt64,
            BsonType.Decimal128          => value.AsDecimal,
            BsonType.String              => value.AsString,
            BsonType.ObjectId            => value.AsObjectId.ToString(),
            BsonType.Null                => null,
            BsonType.Undefined           => null,
            BsonType.Binary              => value.AsBsonBinaryData.Bytes is { Length: > 0 } bytes ? Convert.ToBase64String(bytes) : null,
            BsonType.RegularExpression   => value.AsBsonRegularExpression.Pattern,
            BsonType.JavaScript          => value.AsBsonJavaScript.Code,
            BsonType.Symbol              => value.AsBsonSymbol.Name,
            BsonType.JavaScriptWithScope => value.AsBsonJavaScriptWithScope.Code,
            BsonType.Timestamp           => DateTime.UnixEpoch.AddSeconds(value.AsBsonTimestamp.Timestamp).ToString("o"),
            BsonType.MinKey              => "MinKey",
            BsonType.MaxKey              => "MaxKey",
            _                            => value.ToString()
        };
    }

    internal static JsonObject BsonToJsonObject(BsonDocument doc)
    {
        var jsonObject = new JsonObject();
        foreach (var element in doc.Elements)
        {
            jsonObject[element.Name] = BsonToJsonNode(element.Value);
        }
        return jsonObject;
    }
}