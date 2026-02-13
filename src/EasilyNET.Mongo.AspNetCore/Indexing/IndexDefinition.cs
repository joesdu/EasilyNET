using EasilyNET.Mongo.Core.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace EasilyNET.Mongo.AspNetCore.Indexing;

/// <summary>
/// 索引定义信息
/// </summary>
internal sealed class IndexDefinition
{
    public string Name { get; set; } = string.Empty;

    public BsonDocument Keys { get; set; } = [];

    public bool Unique { get; set; }

    public bool Sparse { get; set; }

    public int? ExpireAfterSeconds { get; set; }

    public Collation? Collation { get; set; }

    public BsonDocument? Weights { get; set; }

    public string? DefaultLanguage { get; set; }

    public EIndexType IndexType { get; set; }

    public string OriginalPath { get; set; } = string.Empty;

#pragma warning disable IDE0046 // 转换为条件表达式
    /// <summary>
    /// 比较两个索引定义是否相同
    /// </summary>
    public bool Equals(IndexDefinition? other)
    {
        if (other is null)
        {
            return false;
        }
        return Name == other.Name &&
               Keys.Equals(other.Keys) &&
               Unique == other.Unique &&
               Sparse == other.Sparse &&
               ExpireAfterSeconds == other.ExpireAfterSeconds &&
               CollationEquals(Collation, other.Collation) &&
               WeightsEquals(Weights, other.Weights) &&
               DefaultLanguage == other.DefaultLanguage;
    }

    private static bool CollationEquals(Collation? c1, Collation? c2)
    {
        if (c1 == null && c2 == null)
        {
            return true;
        }
        if (c1 == null || c2 == null)
        {
            return false;
        }
        return c1.Locale == c2.Locale;
    }

    private static bool WeightsEquals(BsonDocument? w1, BsonDocument? w2)
    {
        if (w1 == null && w2 == null)
        {
            return true;
        }
        if (w1 == null || w2 == null)
        {
            return false;
        }
        return w1.Equals(w2);
    }
#pragma warning restore IDE0046 // 转换为条件表达式
}