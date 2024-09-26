using MongoDB.Driver;

namespace EasilyNET.Mongo.Core
{
    /// <summary>
    /// 标记为时序集合
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TimeSeriesCollectionAttribute : Attribute
    {
        /// <summary>
        /// 时间集合配置
        /// </summary>
        public TimeSeriesOptions TimeSeriesOptions { get; set; }

        /// <summary>
        /// 可选。通过指定文档过期后的秒数，启用自动删除时间序列集合中文档的功能。MongoDB 自动删除过期文档。请参阅设置自动删除时间序列集合 (TTL)，获取更多信息。
        /// </summary>
        public TimeSpan? ExpireAfter { get; set; }

        /// <summary>
        /// 标记为时序集合
        /// </summary>
        /// <param name="timeField">用于时间的顶级字段的名称。</param>
        /// <param name="metaField">描述相关数据分组所依据的系列的顶级字段的名称。</param>
        /// <param name="granularity">表示MongoDB.Driver.TimeSeriesGranularity时间序列的粒度。如果使用bucketMaxSpanSeconds，则不设置</param>
        /// <param name="bucketMaxSpanSeconds">同一存储桶中时间戳之间的最大时间间隔。</param>
        /// <param name="bucketRoundingSeconds">打开新存储桶时用于四舍五入第一个时间戳的间隔。</param>
        public TimeSeriesCollectionAttribute(string timeField, string? metaField = default, TimeSeriesGranularity? granularity = default, int? bucketMaxSpanSeconds = default, int? bucketRoundingSeconds = default)
        {
            TimeSeriesOptions = new TimeSeriesOptions(timeField, metaField, granularity, bucketMaxSpanSeconds, bucketRoundingSeconds);
        }

    }
}
