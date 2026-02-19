namespace EasilyNET.Mongo.Core.Enums;

/// <summary>
///     <para xml:lang="en">Search index type</para>
///     <para xml:lang="zh">搜索索引类型</para>
/// </summary>
public enum ESearchIndexType
{
    /// <summary>
    ///     <para xml:lang="en">Atlas Search (full-text search)</para>
    ///     <para xml:lang="zh">Atlas Search（全文搜索）</para>
    /// </summary>
    Search = 0,

    /// <summary>
    ///     <para xml:lang="en">Atlas Vector Search (semantic/vector search)</para>
    ///     <para xml:lang="zh">Atlas Vector Search（语义/向量搜索）</para>
    /// </summary>
    VectorSearch = 1
}