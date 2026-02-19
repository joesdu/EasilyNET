using EasilyNET.Mongo.Core.Enums;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">
///     Marks a property as a filter field in a Vector Search index.
///     Filter fields allow pre-filtering results before vector similarity is computed.
///     Supported types: <c>boolean</c>, <c>number</c>, <c>string</c>, <c>objectId</c>, <c>date</c>.
///     </para>
///     <para xml:lang="zh">
///     标记一个属性为 Vector Search 索引中的过滤字段。
///     过滤字段允许在计算向量相似度之前预过滤结果。
///     支持的类型：<c>boolean</c>、<c>number</c>、<c>string</c>、<c>objectId</c>、<c>date</c>。
///     </para>
///     <example>
///         <code>
///     [VectorFilterField(IndexName = "vector_index")]
///     public string Category { get; set; }
///         </code>
///     </example>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VectorFilterFieldAttribute : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the vector search index this filter field belongs to.
    ///     Must match a <see cref="MongoSearchIndexAttribute" /> with <see cref="MongoSearchIndexAttribute.Type" />
    ///     set to <see cref="ESearchIndexType.VectorSearch" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此过滤字段所属的向量搜索索引名称。
    ///     必须匹配一个 <see cref="MongoSearchIndexAttribute" />，其 <see cref="MongoSearchIndexAttribute.Type" />
    ///     设置为 <see cref="ESearchIndexType.VectorSearch" />。
    ///     </para>
    /// </summary>
    public string IndexName { get; set; } = "vector_index";
}