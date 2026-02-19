namespace EasilyNET.Mongo.Core.Enums;

/// <summary>
///     <para xml:lang="en">Vector similarity function for vector search indexes</para>
///     <para xml:lang="zh">向量搜索索引的向量相似度函数</para>
/// </summary>
public enum EVectorSimilarity
{
    /// <summary>
    ///     <para xml:lang="en">Cosine similarity — measures the angle between vectors. Best for normalized embeddings.</para>
    ///     <para xml:lang="zh">余弦相似度 — 测量向量之间的角度。最适合归一化嵌入。</para>
    /// </summary>
    Cosine = 0,

    /// <summary>
    ///     <para xml:lang="en">Dot product similarity — measures the projection of one vector onto another. Best for magnitude-sensitive comparisons.</para>
    ///     <para xml:lang="zh">点积相似度 — 测量一个向量在另一个向量上的投影。最适合对幅度敏感的比较。</para>
    /// </summary>
    DotProduct = 1,

    /// <summary>
    ///     <para xml:lang="en">Euclidean distance — measures the straight-line distance between vectors. Best for spatial data.</para>
    ///     <para xml:lang="zh">欧几里得距离 — 测量向量之间的直线距离。最适合空间数据。</para>
    /// </summary>
    Euclidean = 2
}