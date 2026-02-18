using System.Collections;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using MongoDB.Bson;

namespace EasilyNET.Mongo.AspNetCore.SearchIndex;

/// <summary>
/// 从 Attribute 反射信息生成 Atlas Search / Vector Search 索引定义的工厂
/// </summary>
internal static class SearchIndexDefinitionFactory
{
    /// <summary>
    /// 为 Atlas Search 索引生成 BsonDocument 定义
    /// </summary>
    internal static BsonDocument GenerateSearchDefinition(Type type, MongoSearchIndexAttribute indexAttr, bool useCamelCase)
    {
        var fields = new BsonDocument();
        CollectSearchFields(type, useCamelCase, null, indexAttr.Name, fields);
        var mappings = new BsonDocument
        {
            { "dynamic", indexAttr.Dynamic }
        };
        if (fields.ElementCount > 0)
        {
            mappings.Add("fields", fields);
        }
        return new("mappings", mappings);
    }

    /// <summary>
    /// 为 Vector Search 索引生成 BsonDocument 定义
    /// </summary>
    internal static BsonDocument GenerateVectorSearchDefinition(Type type, MongoSearchIndexAttribute indexAttr, bool useCamelCase)
    {
        var fields = new BsonArray();
        CollectVectorFields(type, useCamelCase, null, indexAttr.Name, fields);
        CollectVectorFilterFields(type, useCamelCase, null, indexAttr.Name, fields);
        return new("fields", fields);
    }

    private static void CollectSearchFields(Type type, bool useCamelCase, string? parentPath, string indexName, BsonDocument fields)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";
            var searchAttrs = prop.GetCustomAttributes<SearchFieldAttribute>(false)
                                  .Where(a => a.IndexName == indexName)
                                  .ToList();
            if (searchAttrs.Count > 0)
            {
                if (searchAttrs.Count == 1)
                {
                    fields.Add(fullPath, BuildSearchFieldMapping(searchAttrs[0]));
                }
                else
                {
                    // Multiple mappings for the same field (e.g., string + autocomplete)
                    var mappingsArray = new BsonArray();
                    foreach (var attr in searchAttrs)
                    {
                        mappingsArray.Add(BuildSearchFieldMapping(attr));
                    }
                    fields.Add(fullPath, mappingsArray);
                }
            }

            // Recurse into nested objects (non-primitive, non-collection, non-system types)
            var propType = prop.PropertyType;
            if (propType.IsClass &&
                propType != typeof(string) &&
                propType is { IsArray: false, IsEnum: false } &&
                !typeof(IEnumerable).IsAssignableFrom(propType) &&
                !propType.Assembly.GetName().Name!.StartsWith("System", StringComparison.OrdinalIgnoreCase) &&
                !propType.Assembly.GetName().Name!.StartsWith("MongoDB", StringComparison.OrdinalIgnoreCase))
            {
                CollectSearchFields(propType, useCamelCase, fullPath, indexName, fields);
            }
        }
    }

    private static BsonDocument BuildSearchFieldMapping(SearchFieldAttribute attr)
    {
        var mapping = new BsonDocument("type", MapSearchFieldType(attr.FieldType));
        switch (attr.FieldType)
        {
            case ESearchFieldType.String:
                if (!string.IsNullOrEmpty(attr.AnalyzerName))
                {
                    mapping.Add("analyzer", attr.AnalyzerName);
                }
                if (!string.IsNullOrEmpty(attr.SearchAnalyzerName))
                {
                    mapping.Add("searchAnalyzer", attr.SearchAnalyzerName);
                }
                break;
            case ESearchFieldType.Autocomplete:
                if (!string.IsNullOrEmpty(attr.AnalyzerName))
                {
                    mapping.Add("analyzer", attr.AnalyzerName);
                }
                mapping.Add("maxGrams", attr.MaxGrams);
                mapping.Add("minGrams", attr.MinGrams);
                break;
            case ESearchFieldType.Number:
            case ESearchFieldType.Date:
            case ESearchFieldType.Boolean:
            case ESearchFieldType.ObjectId:
            case ESearchFieldType.Geo:
            case ESearchFieldType.Token:
            case ESearchFieldType.Document:
                // These types have no additional configuration via attributes
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        return mapping;
    }

    private static void CollectVectorFields(Type type, bool useCamelCase, string? parentPath, string indexName, BsonArray fields)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";
            var vectorAttr = prop.GetCustomAttribute<VectorFieldAttribute>(false);
            if (vectorAttr is null || vectorAttr.IndexName != indexName)
            {
                continue;
            }
            if (vectorAttr.Dimensions <= 0)
            {
                throw new InvalidOperationException($"Vector field '{fullPath}' must have Dimensions > 0. Current value: {vectorAttr.Dimensions}");
            }
            fields.Add(new BsonDocument
            {
                { "type", "vector" },
                { "path", fullPath },
                { "numDimensions", vectorAttr.Dimensions },
                { "similarity", MapVectorSimilarity(vectorAttr.Similarity) }
            });
        }
    }

    private static void CollectVectorFilterFields(Type type, bool useCamelCase, string? parentPath, string indexName, BsonArray fields)
    {
        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            var fieldName = useCamelCase ? prop.Name.ToLowerCamelCase() : prop.Name;
            var fullPath = string.IsNullOrEmpty(parentPath) ? fieldName : $"{parentPath}.{fieldName}";
            var filterAttr = prop.GetCustomAttribute<VectorFilterFieldAttribute>(false);
            if (filterAttr is not null && filterAttr.IndexName == indexName)
            {
                fields.Add(new BsonDocument
                {
                    { "type", "filter" },
                    { "path", fullPath }
                });
            }
        }
    }

    private static string MapSearchFieldType(ESearchFieldType fieldType) =>
        fieldType switch
        {
            ESearchFieldType.String       => "string",
            ESearchFieldType.Number       => "number",
            ESearchFieldType.Date         => "date",
            ESearchFieldType.Boolean      => "boolean",
            ESearchFieldType.ObjectId     => "objectId",
            ESearchFieldType.Geo          => "geo",
            ESearchFieldType.Autocomplete => "autocomplete",
            ESearchFieldType.Token        => "token",
            ESearchFieldType.Document     => "embeddedDocuments",
            _                             => throw new NotSupportedException($"Unsupported search field type: {fieldType}")
        };

    private static string MapVectorSimilarity(EVectorSimilarity similarity) =>
        similarity switch
        {
            EVectorSimilarity.Cosine     => "cosine",
            EVectorSimilarity.DotProduct => "dotProduct",
            EVectorSimilarity.Euclidean  => "euclidean",
            _                            => throw new NotSupportedException($"Unsupported vector similarity: {similarity}")
        };
}