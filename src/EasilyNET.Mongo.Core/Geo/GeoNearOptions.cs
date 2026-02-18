using MongoDB.Bson;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace EasilyNET.Mongo.Core.Geo;

/// <summary>
///     <para xml:lang="en">Options for the <c>$geoNear</c> aggregation stage</para>
///     <para xml:lang="zh"><c>$geoNear</c> 聚合阶段的选项</para>
/// </summary>
public sealed class GeoNearOptions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     The output field that contains the calculated distance.
    ///     Defaults to <c>"distance"</c>.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     包含计算距离的输出字段。默认为 <c>"distance"</c>。
    ///     </para>
    /// </summary>
    public string DistanceField { get; set; } = "distance";

    /// <summary>
    ///     <para xml:lang="en">
    ///     Whether to calculate distances using spherical geometry.
    ///     Defaults to <see langword="true" />.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     是否使用球面几何计算距离。默认为 <see langword="true" />。
    ///     </para>
    /// </summary>
    public bool Spherical { get; set; } = true;

    /// <summary>
    ///     <para xml:lang="en">
    ///     The maximum distance from the center point in meters.
    ///     Optional. If not set, no maximum distance limit is applied.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     距中心点的最大距离（米）。可选。如果未设置，则不应用最大距离限制。
    ///     </para>
    /// </summary>
    public double? MaxDistanceMeters { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     The minimum distance from the center point in meters.
    ///     Optional. If not set, no minimum distance limit is applied.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     距中心点的最小距离（米）。可选。如果未设置，则不应用最小距离限制。
    ///     </para>
    /// </summary>
    public double? MinDistanceMeters { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Maximum number of documents to return. Optional.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     要返回的最大文档数。可选。
    ///     </para>
    /// </summary>
    public int? Limit { get; set; }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Additional query filter to apply before the distance calculation.
    ///     Optional.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     在距离计算之前应用的附加查询过滤器。可选。
    ///     </para>
    /// </summary>
    public BsonDocument? Filter { get; set; }
}