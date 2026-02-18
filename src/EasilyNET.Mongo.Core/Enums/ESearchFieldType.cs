namespace EasilyNET.Mongo.Core.Enums;

/// <summary>
///     <para xml:lang="en">Search field mapping type for Atlas Search index definitions</para>
///     <para xml:lang="zh">Atlas Search 索引定义的搜索字段映射类型</para>
/// </summary>
public enum ESearchFieldType
{
    /// <summary>
    ///     <para xml:lang="en">String field type — supports text search, phrase, regex</para>
    ///     <para xml:lang="zh">字符串字段类型 — 支持文本搜索、短语、正则表达式</para>
    /// </summary>
    String = 0,

    /// <summary>
    ///     <para xml:lang="en">Number field type — supports range queries, near, equals</para>
    ///     <para xml:lang="zh">数字字段类型 — 支持范围查询、近似、等于</para>
    /// </summary>
    Number = 1,

    /// <summary>
    ///     <para xml:lang="en">Date field type — supports date range queries</para>
    ///     <para xml:lang="zh">日期字段类型 — 支持日期范围查询</para>
    /// </summary>
    Date = 2,

    /// <summary>
    ///     <para xml:lang="en">Boolean field type — supports equals queries</para>
    ///     <para xml:lang="zh">布尔字段类型 — 支持等于查询</para>
    /// </summary>
    Boolean = 3,

    /// <summary>
    ///     <para xml:lang="en">ObjectId field type — supports equals queries</para>
    ///     <para xml:lang="zh">ObjectId 字段类型 — 支持等于查询</para>
    /// </summary>
    ObjectId = 4,

    /// <summary>
    ///     <para xml:lang="en">Geo field type — supports geoShape and geoWithin queries</para>
    ///     <para xml:lang="zh">地理字段类型 — 支持 geoShape 和 geoWithin 查询</para>
    /// </summary>
    Geo = 5,

    /// <summary>
    ///     <para xml:lang="en">Autocomplete field type — supports autocomplete queries with partial input</para>
    ///     <para xml:lang="zh">自动补全字段类型 — 支持部分输入的自动补全查询</para>
    /// </summary>
    Autocomplete = 6,

    /// <summary>
    ///     <para xml:lang="en">Token field type — supports exact match on string tokens (no analysis)</para>
    ///     <para xml:lang="zh">Token 字段类型 — 支持字符串令牌的精确匹配（无分析）</para>
    /// </summary>
    Token = 7,

    /// <summary>
    ///     <para xml:lang="en">Document field type — supports searching within embedded documents</para>
    ///     <para xml:lang="zh">文档字段类型 — 支持在嵌入文档中搜索</para>
    /// </summary>
    Document = 8
}