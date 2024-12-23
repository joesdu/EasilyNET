using MongoDB.Driver;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.Mongo.Core;

/// <summary>
///     <para xml:lang="en">Marked as a time series collection</para>
///     <para xml:lang="zh">标记为时序集合</para>
///     <see href="https://www.mongodb.com/zh-cn/docs/rapid/core/timeseries/timeseries-procedures" />
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class TimeSeriesCollectionAttribute : Attribute
{
    /// <summary>
    ///     <para xml:lang="en">Marked as a time series collection</para>
    ///     <para xml:lang="zh">标记为时序集合</para>
    /// </summary>
    /// <param name="collectionName">
    ///     <para xml:lang="en">The name of the collection to create.</para>
    ///     <para xml:lang="zh">创建集合的名称。</para>
    /// </param>
    /// <param name="timeField">
    ///     <para xml:lang="en">The name of the top-level field for time.</para>
    ///     <para xml:lang="zh">用于时间的顶级字段的名称。</para>
    /// </param>
    /// <param name="metaField">
    ///     <para xml:lang="en">The name of the top-level field that describes the series on which related data is grouped.</para>
    ///     <para xml:lang="zh">描述相关数据分组所依据的系列的顶级字段的名称。</para>
    /// </param>
    /// <param name="granularity">
    ///     <para xml:lang="en">Represents the granularity of the time series. If using bucketMaxSpanSeconds, do not set this.</para>
    ///     <para xml:lang="zh">表示时间序列的粒度。如果使用 bucketMaxSpanSeconds，则不设置。</para>
    /// </param>
    public TimeSeriesCollectionAttribute(string collectionName, string timeField, string metaField, TimeSeriesGranularity granularity = TimeSeriesGranularity.Seconds)
    {
        CollectionName = collectionName;
        TimeSeriesOptions = new(timeField, metaField, granularity, null, null);
    }

    /// <summary>
    ///     <para xml:lang="en">Marked as a time series collection</para>
    ///     <para xml:lang="zh">标记为时序集合</para>
    /// </summary>
    /// <param name="collectionName">
    ///     <para xml:lang="en">The name of the collection to create.</para>
    ///     <para xml:lang="zh">创建集合的名称。</para>
    /// </param>
    /// <param name="timeField">
    ///     <para xml:lang="en">The name of the top-level field for time.</para>
    ///     <para xml:lang="zh">用于时间的顶级字段的名称。</para>
    /// </param>
    /// <param name="metaField">
    ///     <para xml:lang="en">The name of the top-level field that describes the series on which related data is grouped.</para>
    ///     <para xml:lang="zh">描述相关数据分组所依据的系列的顶级字段的名称。</para>
    /// </param>
    /// <param name="bucketMaxSpanSeconds">
    ///     <para xml:lang="en">The maximum time span between timestamps in the same bucket.</para>
    ///     <para xml:lang="zh">同一存储桶中时间戳之间的最大时间间隔。</para>
    /// </param>
    /// <param name="bucketRoundingSeconds">
    ///     <para xml:lang="en">The interval used to round the first timestamp when opening a new bucket.</para>
    ///     <para xml:lang="zh">打开新存储桶时用于四舍五入第一个时间戳的间隔。</para>
    /// </param>
    public TimeSeriesCollectionAttribute(string collectionName, string timeField, string metaField, int bucketMaxSpanSeconds, int bucketRoundingSeconds)
    {
        CollectionName = collectionName;
        TimeSeriesOptions = new(timeField, metaField, null, bucketMaxSpanSeconds, bucketRoundingSeconds);
    }

    /// <summary>
    ///     <para xml:lang="en">The name of the collection to create</para>
    ///     <para xml:lang="zh">创建集合的名称</para>
    /// </summary>
    public string CollectionName { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">Time series options</para>
    ///     <para xml:lang="zh">时间集合配置</para>
    /// </summary>
    public TimeSeriesOptions TimeSeriesOptions { get; private set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Optional. Enable automatic deletion of documents in the time series collection by specifying the number of seconds after
    ///     which the documents expire. MongoDB automatically deletes expired documents. See Setting TTL for Time Series Collections for more information.
    ///     </para>
    ///     <para xml:lang="zh">可选。通过指定文档过期后的秒数，启用自动删除时间序列集合中文档的功能。MongoDB 自动删除过期文档。请参阅设置自动删除时间序列集合 (TTL)，获取更多信息。</para>
    /// </summary>
    public TimeSpan? ExpireAfter { get; set; }
}