// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">
///     Marks a text property for MongoDB Atlas Vector Search automated embedding.
///     Atlas automatically generates vector embeddings for this field using the specified Voyage AI model
///     at index time, and for query strings at query time — no manual embedding pipeline required.
///     The property type should be <c>string</c>.
///     Emitted as <c>{ "type": "autoEmbed", "modality": "text", "path": ..., "model": ... }</c> in the index definition.
///     </para>
///     <para xml:lang="zh">
///     标记一个文本属性用于 MongoDB Atlas Vector Search 自动嵌入。
///     Atlas 会在索引时使用指定的 Voyage AI 模型自动为该字段生成向量嵌入，
///     并在查询时自动为查询字符串生成嵌入 — 无需手动维护嵌入管道。
///     属性类型应为 <c>string</c>。
///     在索引定义中输出为 <c>{ "type": "autoEmbed", "modality": "text", "path": ..., "model": ... }</c>。
///     </para>
///     <example>
///         <code>
///     [MongoSearchIndex(Name = "auto_vector", Type = ESearchIndexType.VectorSearch)]
///     public class Article
///     {
///         [AutoEmbeddingField("voyage-4", IndexName = "auto_vector")]
///         public string Content { get; set; }
///     }
///         </code>
///     </example>
/// </summary>
/// <param name="model">
///     <para xml:lang="en">The Voyage AI embedding model used to generate embeddings, e.g. <c>"voyage-4"</c> or <c>"voyage-3-large"</c></para>
///     <para xml:lang="zh">用于生成嵌入的 Voyage AI 嵌入模型，例如 <c>"voyage-4"</c> 或 <c>"voyage-3-large"</c></para>
/// </param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class AutoEmbeddingFieldAttribute(string model) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The Voyage AI embedding model used to generate embeddings, e.g. <c>"voyage-4"</c> or <c>"voyage-3-large"</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     用于生成嵌入的 Voyage AI 嵌入模型，例如 <c>"voyage-4"</c> 或 <c>"voyage-3-large"</c>。
    ///     </para>
    /// </summary>
    public string Model { get; } = model;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the vector search index this field belongs to.
    ///     Must match a <see cref="MongoSearchIndexAttribute" /> with <see cref="MongoSearchIndexAttribute.Type" />
    ///     set to <see cref="Enums.ESearchIndexType.VectorSearch" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此字段所属的向量搜索索引名称。
    ///     必须匹配一个 <see cref="MongoSearchIndexAttribute" />，其 <see cref="MongoSearchIndexAttribute.Type" />
    ///     设置为 <see cref="Enums.ESearchIndexType.VectorSearch" />。
    ///     </para>
    /// </summary>
    public string IndexName { get; set; } = "vector_index";
}
