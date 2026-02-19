using EasilyNET.Mongo.Core.Enums;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core.Attributes;

/// <summary>
///     <para xml:lang="en">
///     Marks a property as a field in an Atlas Search index definition.
///     Multiple attributes can be applied to the same property to index it with different field types
///     (e.g., both <see cref="ESearchFieldType.String" /> and <see cref="ESearchFieldType.Autocomplete" />).
///     </para>
///     <para xml:lang="zh">
///     标记一个属性为 Atlas Search 索引定义中的字段。
///     可以在同一属性上应用多个特性以使用不同的字段类型进行索引
///     （例如同时使用 <see cref="ESearchFieldType.String" /> 和 <see cref="ESearchFieldType.Autocomplete" />）。
///     </para>
///     <example>
///         <code>
///     [SearchField(ESearchFieldType.String, AnalyzerName = "lucene.chinese")]
///     [SearchField(ESearchFieldType.Autocomplete, AnalyzerName = "lucene.chinese")]
///     public string Title { get; set; }
///         </code>
///     </example>
/// </summary>
/// <param name="fieldType">
///     <para xml:lang="en">The search field mapping type</para>
///     <para xml:lang="zh">搜索字段映射类型</para>
/// </param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public sealed class SearchFieldAttribute(ESearchFieldType fieldType) : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">The search field mapping type</para>
    ///     <para xml:lang="zh">搜索字段映射类型</para>
    /// </summary>
    public ESearchFieldType FieldType { get; } = fieldType;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The name of the search index this field belongs to.
    ///     Defaults to <c>"default"</c>. Use this to associate fields with specific named indexes.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     此字段所属的搜索索引名称。
    ///     默认为 <c>"default"</c>。使用此属性将字段与特定命名索引关联。
    ///     </para>
    /// </summary>
    public string IndexName { get; set; } = "default";

    /// <summary>
    ///     <para xml:lang="en">
    ///     The analyzer to use for this field. Only applicable to <see cref="ESearchFieldType.String" />
    ///     and <see cref="ESearchFieldType.Autocomplete" /> field types.
    ///     Common values: <c>"lucene.standard"</c>, <c>"lucene.chinese"</c>, <c>"lucene.simple"</c>, <c>"lucene.keyword"</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     用于此字段的分析器。仅适用于 <see cref="ESearchFieldType.String" />
    ///     和 <see cref="ESearchFieldType.Autocomplete" /> 字段类型。
    ///     常用值：<c>"lucene.standard"</c>、<c>"lucene.chinese"</c>、<c>"lucene.simple"</c>、<c>"lucene.keyword"</c>。
    ///     </para>
    /// </summary>
    public string? AnalyzerName { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     The search analyzer to use at query time. If not set, the <see cref="AnalyzerName" /> is used.
    ///     Only applicable to <see cref="ESearchFieldType.String" /> field type.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     查询时使用的搜索分析器。如果未设置，则使用 <see cref="AnalyzerName" />。
    ///     仅适用于 <see cref="ESearchFieldType.String" /> 字段类型。
    ///     </para>
    /// </summary>
    public string? SearchAnalyzerName { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Maximum number of grams for autocomplete. Only applicable to <see cref="ESearchFieldType.Autocomplete" />.
    ///     Defaults to <c>15</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     自动补全的最大 gram 数。仅适用于 <see cref="ESearchFieldType.Autocomplete" />。
    ///     默认为 <c>15</c>。
    ///     </para>
    /// </summary>
    public int MaxGrams { get; set; } = 15;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Minimum number of grams for autocomplete. Only applicable to <see cref="ESearchFieldType.Autocomplete" />.
    ///     Defaults to <c>2</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     自动补全的最小 gram 数。仅适用于 <see cref="ESearchFieldType.Autocomplete" />。
    ///     默认为 <c>2</c>。
    ///     </para>
    /// </summary>
    public int MinGrams { get; set; } = 2;
}