using EasilyNET.Mongo.Core.Enums;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">
///     Marks a property as a vector field in a MongoDB Atlas Vector Search index.
///     The property type should be <c>float[]</c>, <c>double[]</c>, or <c>ReadOnlyMemory&lt;float&gt;</c>.
///     </para>
///     <para xml:lang="zh">
///     标记一个属性为 MongoDB Atlas Vector Search 索引中的向量字段。
///     属性类型应为 <c>float[]</c>、<c>double[]</c> 或 <c>ReadOnlyMemory&lt;float&gt;</c>。
///     </para>
///     <example>
///         <code>
///     [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine)]
///     public float[] Embedding { get; set; }
///         </code>
///     </example>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class VectorFieldAttribute : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The number of dimensions of the vector. Must match the output dimensions of your embedding model.
    ///     For example, OpenAI <c>text-embedding-ada-002</c> outputs 1536 dimensions.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     向量的维度数。必须与嵌入模型的输出维度匹配。
    ///     例如，OpenAI <c>text-embedding-ada-002</c> 输出 1536 维。
    ///     </para>
    /// </summary>
    public int Dimensions { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     The similarity function to use for vector comparisons.
    ///     Defaults to <see cref="EVectorSimilarity.Cosine" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     用于向量比较的相似度函数。
    ///     默认为 <see cref="EVectorSimilarity.Cosine" />。
    ///     </para>
    /// </summary>
    public EVectorSimilarity Similarity { get; set; } = EVectorSimilarity.Cosine;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the vector search index this field belongs to.
    ///     Must match a <see cref="MongoSearchIndexAttribute" /> with <see cref="MongoSearchIndexAttribute.Type" />
    ///     set to <see cref="ESearchIndexType.VectorSearch" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此字段所属的向量搜索索引名称。
    ///     必须匹配一个 <see cref="MongoSearchIndexAttribute" />，其 <see cref="MongoSearchIndexAttribute.Type" />
    ///     设置为 <see cref="ESearchIndexType.VectorSearch" />。
    ///     </para>
    /// </summary>
    public string IndexName { get; set; } = "vector_index";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatic vector quantization type. <see cref="EVectorQuantization.Scalar" /> reduces memory by ~4x,
    ///     <see cref="EVectorQuantization.Binary" /> by ~32x.
    ///     Defaults to <see cref="EVectorQuantization.None" /> (full-fidelity vectors, no quantization field emitted).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     自动向量量化类型。<see cref="EVectorQuantization.Scalar" /> 约降低 4 倍内存占用，
    ///     <see cref="EVectorQuantization.Binary" /> 约降低 32 倍。
    ///     默认为 <see cref="EVectorQuantization.None" />（全精度向量，不输出 quantization 字段）。
    ///     </para>
    /// </summary>
    public EVectorQuantization Quantization { get; set; } = EVectorQuantization.None;

    /// <summary>
    ///     <para xml:lang="en">
    ///     HNSW graph option: the maximum number of edges (connected neighbors) per node in the graph.
    ///     Higher values improve recall at the cost of indexing speed and memory.
    ///     Set to <c>0</c> (default) to use the Atlas server default.
    ///     Emitted as <c>hnswOptions.maxEdges</c> in the index definition.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     HNSW 图选项：图中每个节点的最大边数（相连邻居数）。
    ///     数值越大召回率越高，但索引速度和内存开销也随之增加。
    ///     设置为 <c>0</c>（默认）时使用 Atlas 服务端默认值。
    ///     在索引定义中输出为 <c>hnswOptions.maxEdges</c>。
    ///     </para>
    /// </summary>
    public int MaxEdges { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     HNSW graph option: the number of nearest-neighbor candidates examined during index construction.
    ///     Higher values improve index quality at the cost of build time.
    ///     Set to <c>0</c> (default) to use the Atlas server default.
    ///     Emitted as <c>hnswOptions.numEdgeCandidates</c> in the index definition.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     HNSW 图选项：索引构建期间检查的最近邻候选数量。
    ///     数值越大索引质量越高，但构建时间越长。
    ///     设置为 <c>0</c>（默认）时使用 Atlas 服务端默认值。
    ///     在索引定义中输出为 <c>hnswOptions.numEdgeCandidates</c>。
    ///     </para>
    /// </summary>
    public int NumEdgeCandidates { get; set; }
}