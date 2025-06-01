using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
/// 单个字段索引特性
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
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
}