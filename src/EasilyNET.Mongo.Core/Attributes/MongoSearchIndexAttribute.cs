using EasilyNET.Mongo.Core.Enums;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">
///     Marks a class as requiring a MongoDB Atlas Search or Vector Search index.
///     Multiple attributes can be applied to create multiple search indexes on the same collection.
///     </para>
///     <para xml:lang="zh">
///     标记一个类需要 MongoDB Atlas Search 或 Vector Search 索引。
///     可以应用多个特性以在同一集合上创建多个搜索索引。
///     </para>
///     <example>
///         <code>
///     [MongoSearchIndex(Name = "default")]
///     [MongoSearchIndex(Name = "vector_index", Type = ESearchIndexType.VectorSearch)]
///     public class Article
///     {
///         [SearchField(ESearchFieldType.String, AnalyzerName = "lucene.chinese")]
///         public string Title { get; set; }
/// 
///         [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine, IndexName = "vector_index")]
///         public float[] Embedding { get; set; }
///     }
///         </code>
///     </example>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MongoSearchIndexAttribute : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the search index. Defaults to <c>"default"</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     搜索索引的名称。默认为 <c>"default"</c>。
    ///     </para>
    /// </summary>
    public string Name { get; set; } = "default";

    /// <summary>
    ///     <para xml:lang="en">
    ///     The type of search index. Defaults to <see cref="ESearchIndexType.Search" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     搜索索引的类型。默认为 <see cref="ESearchIndexType.Search" />。
    ///     </para>
    /// </summary>
    public ESearchIndexType Type { get; set; } = ESearchIndexType.Search;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to enable dynamic field mapping. When <see langword="true" />, all fields are automatically indexed.
    ///     When <see langword="false" />, only fields with <see cref="SearchFieldAttribute" /> are indexed.
    ///     Defaults to <see langword="false" />.
    ///     Only applies to <see cref="ESearchIndexType.Search" /> indexes.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否启用动态字段映射。当为 <see langword="true" /> 时，所有字段自动索引。
    ///     当为 <see langword="false" /> 时，仅索引标记了 <see cref="SearchFieldAttribute" /> 的字段。
    ///     默认为 <see langword="false" />。
    ///     仅适用于 <see cref="ESearchIndexType.Search" /> 索引。
    ///     </para>
    /// </summary>
    public bool Dynamic { get; set; }
}