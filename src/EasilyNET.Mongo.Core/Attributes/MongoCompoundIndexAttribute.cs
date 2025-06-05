using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
/// 复合索引特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class MongoCompoundIndexAttribute : Attribute
{
    /// <inheritdoc />
    public MongoCompoundIndexAttribute(string[] fields, EIndexType[] types)
    {
        if (fields.Length != types.Length)
        {
            throw new ArgumentException("Fields and Types must have the same length", nameof(fields));
        }
        Fields = fields;
        Types = types;
    }

    /// <summary>
    /// Gets the names of the fields that are part of the composite index.
    /// </summary>
    public string[] Fields { get; }

    /// <summary>
    /// Gets the array of index types associated with each field.
    /// </summary>
    public EIndexType[] Types { get; }

    /// <summary>
    /// Gets or sets the name of the index.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the index is unique.
    /// </summary>
    public bool Unique { get; set; }

    /// <summary>
    /// 是否为稀疏索引
    /// </summary>
    public bool Sparse { get; set; }

    /// <summary>
    /// TTL索引的过期秒数（仅对TTL索引有效）
    /// </summary>
    public int? ExpireAfterSeconds { get; set; }

    /// <summary>
    /// 排序规则（Collation，json字符串）
    /// </summary>
    public string? Collation { get; set; }
}