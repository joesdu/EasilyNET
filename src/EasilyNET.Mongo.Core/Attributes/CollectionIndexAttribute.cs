using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
/// 索引基础特性
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CollectionIndexAttribute : Attribute
{
    /// <summary>
    /// 索引名称
    /// </summary>
    public string? IndexName { get; set; } = string.Empty;

    /// <summary>
    /// 是否是复合索引
    /// </summary>
    public bool IsCompound { get; set; }

    /// <summary>
    /// 索引类型
    /// </summary>
    public EIndexType IndexType { get; set; } = EIndexType.Ascending;
}