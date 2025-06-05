using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
/// 单个字段索引特性
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class MongoIndexAttribute(EIndexType type) : Attribute
{
    /// <summary>
    /// Gets the type of the index represented by this instance.
    /// </summary>
    public EIndexType Type { get; } = type;

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

    /// <summary>
    /// 文本索引选项（如 weights、default_language，JSON字符串）
    /// </summary>
    public string? TextIndexOptions { get; set; }
}