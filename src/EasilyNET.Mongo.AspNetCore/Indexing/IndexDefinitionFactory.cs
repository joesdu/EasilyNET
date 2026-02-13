using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace EasilyNET.Mongo.AspNetCore.Indexing;

/// <summary>
/// 索引定义工厂，负责根据特性信息创建各类索引定义
/// </summary>
internal static class IndexDefinitionFactory
{
    /// <summary>
    /// 生成当前类型需要的所有索引定义
    /// </summary>
    internal static List<IndexDefinition> GenerateRequiredIndexes(Type type, string collectionName, bool useCamelCase, bool isTimeSeries, HashSet<string>? timeSeriesFields = null)
    {
        var requiredIndexes = new List<IndexDefinition>();
        var allIndexFields = new List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)>();
        var allTextFields = new List<string>();
        var allWildcardFields = new List<(string Path, MongoIndexAttribute Attr)>();
        // 收集所有索引字段
        IndexFieldCollector.CollectIndexFields(type, useCamelCase, null, allIndexFields, allTextFields, allWildcardFields, timeSeriesFields);
        // 验证文本索引
        IndexFieldCollector.ValidateTextIndexes(allIndexFields, allTextFields);
        // 生成单字段索引
        foreach (var (path, attr, declaringType) in allIndexFields.Where(x => x.Attr.Type != EIndexType.Text))
        {
            var indexDef = CreateSingleFieldIndex(path, attr, declaringType, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成通配符索引
        foreach (var (path, attr) in allWildcardFields)
        {
            var indexDef = CreateWildcardIndex(path, attr, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成文本索引
        if (allTextFields.Count > 0)
        {
            var indexDef = CreateTextIndex(allTextFields, allIndexFields, collectionName, isTimeSeries);
            requiredIndexes.Add(indexDef);
        }
        // 生成复合索引
        var compoundIndexes = type.GetCustomAttributes<MongoCompoundIndexAttribute>(false);
        requiredIndexes.AddRange(compoundIndexes.Select(compoundAttr => CreateCompoundIndex(compoundAttr, type, collectionName, useCamelCase, isTimeSeries)));
        return requiredIndexes;
    }

    /// <summary>
    /// 创建单字段索引定义
    /// </summary>
    private static IndexDefinition CreateSingleFieldIndex(string path, MongoIndexAttribute attr, Type declaringType, string collectionName, bool isTimeSeries = false)
    {
        var indexName = attr.Name ?? GenerateIndexName(collectionName, path, attr.Type.ToString());
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        // TTL 索引类型验证
        if (attr.ExpireAfterSeconds.HasValue)
        {
            var propertyType = IndexFieldCollector.GetNestedPropertyType(declaringType, path.Replace('_', '.'));
            if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
            {
                throw new InvalidOperationException($"TTL 索引字段 '{path}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
            }
        }
        var keys = attr.Type switch
        {
            EIndexType.Ascending   => new(path, 1),
            EIndexType.Descending  => new(path, -1),
            EIndexType.Geo2D       => new(path, "2d"),
            EIndexType.Geo2DSphere => new(path, "2dsphere"),
            EIndexType.Hashed      => new(path, "hashed"),
            EIndexType.Multikey    => new(path, 1),                       // Multikey 自动识别
            EIndexType.Text        => new(path, "text"),                  // Text 索引
            EIndexType.Wildcard    => new BsonDocument(path, "wildcard"), // Wildcard 索引
            _                      => throw new NotSupportedException($"不支持的索引类型 {attr.Type}")
        };
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = keys,
            Unique = attr.Unique,
            Sparse = ResolveSparse(attr.Sparse, isTimeSeries),
            ExpireAfterSeconds = attr.ExpireAfterSeconds,
            IndexType = attr.Type,
            OriginalPath = path
        };
        ParseCollation(indexDef, attr.Collation, indexName);
        return indexDef;
    }

    /// <summary>
    /// 创建通配符索引定义
    /// </summary>
    private static IndexDefinition CreateWildcardIndex(string path, MongoIndexAttribute attr, string collectionName, bool isTimeSeries = false)
    {
        var wildcardPath = path.EndsWith("$**") ? path : $"{path}.$**";
        if (!wildcardPath.Contains("$**"))
        {
            throw new InvalidOperationException($"通配符索引路径 '{path}' 格式无效，应包含 '$**' 通配符。");
        }
        var indexName = attr.Name ?? GenerateIndexName(collectionName, wildcardPath, "Wildcard");
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = new(wildcardPath, "$**"),
            Unique = attr.Unique,
            Sparse = ResolveSparse(attr.Sparse, isTimeSeries),
            IndexType = EIndexType.Wildcard,
            OriginalPath = wildcardPath
        };
        ParseCollation(indexDef, attr.Collation, indexName);
        return indexDef;
    }

    /// <summary>
    /// 创建文本索引定义
    /// </summary>
    private static IndexDefinition CreateTextIndex(List<string> textFields, List<(string Path, MongoIndexAttribute Attr, Type DeclaringType)> allIndexFields, string collectionName, bool isTimeSeries = false)
    {
        var textIndexName = $"{collectionName}_" + string.Join("_", textFields) + "_Text";
        if (textIndexName.Length > 127)
        {
            textIndexName = TruncateIndexName(textIndexName, 127);
        }
        var keys = new BsonDocument();
        foreach (var field in textFields)
        {
            keys.Add(field, "text");
        }
        var firstTextAttr = allIndexFields.FirstOrDefault(x => x.Attr.Type == EIndexType.Text).Attr ?? throw new InvalidOperationException($"文本索引字段已收集但未找到对应的文本索引特性，集合: {collectionName}");
        var indexDef = new IndexDefinition
        {
            Name = textIndexName,
            Keys = keys,
            Unique = false, // 文本索引不支持唯一性
            Sparse = ResolveSparse(firstTextAttr.Sparse, isTimeSeries),
            IndexType = EIndexType.Text,
            OriginalPath = string.Join(",", textFields)
        };
        ParseCollation(indexDef, firstTextAttr.Collation, textIndexName);
        // 解析文本索引选项
        // ReSharper disable once InvertIf
        if (!string.IsNullOrWhiteSpace(firstTextAttr.TextIndexOptions))
        {
            try
            {
                var textOptionsDoc = BsonSerializer.Deserialize<BsonDocument>(firstTextAttr.TextIndexOptions);
                if (textOptionsDoc.Contains("weights"))
                {
                    indexDef.Weights = textOptionsDoc["weights"].AsBsonDocument;
                }
                if (textOptionsDoc.Contains("default_language"))
                {
                    indexDef.DefaultLanguage = textOptionsDoc["default_language"].AsString;
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"文本索引 '{textIndexName}' 的选项 JSON 无效: {firstTextAttr.TextIndexOptions}", ex);
            }
        }
        return indexDef;
    }

    /// <summary>
    /// 创建复合索引定义
    /// </summary>
    private static IndexDefinition CreateCompoundIndex(MongoCompoundIndexAttribute compoundAttr, Type type, string collectionName, bool useCamelCase, bool isTimeSeries = false)
    {
        var fields = compoundAttr.Fields.Select(f => useCamelCase ? f.ToLowerCamelCase() : f).ToArray();
        var indexName = compoundAttr.Name ?? $"{collectionName}_{string.Join("_", fields)}";
        if (indexName.Length > 127)
        {
            indexName = TruncateIndexName(indexName, 127);
        }
        // TTL 索引类型验证
        if (compoundAttr.ExpireAfterSeconds.HasValue)
        {
            foreach (var field in compoundAttr.Fields)
            {
                var propertyType = IndexFieldCollector.GetNestedPropertyType(type, field);
                if (propertyType == null || (propertyType != typeof(DateTime) && propertyType != typeof(DateTime?) && propertyType != typeof(BsonDateTime)))
                {
                    throw new InvalidOperationException($"复合索引 '{indexName}' 的 TTL 字段 '{field}' 必须为 DateTime、DateTime? 或 BsonDateTime 类型。当前类型: {propertyType?.Name ?? "未知"}");
                }
            }
        }
        var keys = new BsonDocument();
        for (var i = 0; i < fields.Length; i++)
        {
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            object typeVal = compoundAttr.Types[i] switch
            {
                EIndexType.Ascending   => 1,
                EIndexType.Descending  => -1,
                EIndexType.Geo2D       => "2d",
                EIndexType.Geo2DSphere => "2dsphere",
                EIndexType.Hashed      => "hashed",
                EIndexType.Text        => "text",
                _                      => throw new NotSupportedException($"不支持的索引类型 {compoundAttr.Types[i]}")
            };
            keys.Add(fields[i], BsonValue.Create(typeVal));
        }
        var indexDef = new IndexDefinition
        {
            Name = indexName,
            Keys = keys,
            Unique = compoundAttr.Unique,
            Sparse = ResolveSparse(compoundAttr.Sparse, isTimeSeries),
            ExpireAfterSeconds = compoundAttr.ExpireAfterSeconds,
            IndexType = EIndexType.Ascending, // 复合索引使用默认类型
            OriginalPath = string.Join(",", fields)
        };
        ParseCollation(indexDef, compoundAttr.Collation, indexName);
        return indexDef;
    }

    /// <summary>
    /// 解析排序规则 JSON 并设置到索引定义上（消除 4 处重复的 Collation 解析逻辑）
    /// </summary>
    private static void ParseCollation(IndexDefinition indexDef, string? collationJson, string indexName)
    {
        if (string.IsNullOrWhiteSpace(collationJson))
        {
            return;
        }
        try
        {
            var collationDoc = BsonSerializer.Deserialize<BsonDocument>(collationJson);
            var locale = collationDoc.GetValue("locale", null)?.AsString;
            if (!string.IsNullOrEmpty(locale))
            {
                indexDef.Collation = new(locale);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"索引 '{indexName}' 的排序规则 JSON 无效: {collationJson}", ex);
        }
    }

    /// <summary>
    /// 解析稀疏索引设置（时序集合强制禁用稀疏索引）
    /// </summary>
    private static bool ResolveSparse(bool sparse, bool isTimeSeries) => !isTimeSeries && sparse;

    /// <summary>
    /// 生成索引名称
    /// </summary>
    private static string GenerateIndexName(string collectionName, string fieldPath, string indexType)
    {
        // 清理路径中的特殊字符
        var cleanPath = fieldPath.Replace("$", "").Replace("*", "").Replace(".", "_");
        return $"{collectionName}_{cleanPath}_{indexType}";
    }

    /// <summary>
    /// 截断索引名称以符合MongoDB限制
    /// </summary>
    private static string TruncateIndexName(string indexName, int maxLength)
    {
        if (indexName.Length <= maxLength)
        {
            return indexName;
        }
        // 保留前缀和后缀，中间用哈希值填充
        var prefix = indexName[..(maxLength / 3)];
        var suffix = indexName[^(maxLength / 3)..];
        var hash = indexName.To32MD5()[..Math.Min(8, maxLength - prefix.Length - suffix.Length)];
        return $"{prefix}_{hash}_{suffix}";
    }
}