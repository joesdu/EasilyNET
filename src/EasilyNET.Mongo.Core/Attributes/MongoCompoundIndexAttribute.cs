using EasilyNET.Mongo.Core.Enums;

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
/// 复合索引特性
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
// ReSharper disable once ClassNeverInstantiated.Global
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
}