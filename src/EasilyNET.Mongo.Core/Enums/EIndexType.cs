namespace EasilyNET.Mongo.Core.Enums;

/// <summary>
/// 索引类型
/// </summary>
public enum EIndexType
{
    /// <summary>
    /// 升序
    /// <para xml:lang="en">Ascending</para>
    /// </summary>
    Ascending = 0,

    /// <summary>
    /// 降序
    /// <para xml:lang="en">Descending</para>
    /// </summary>
    Descending = 1,

    /// <summary>
    /// 2D 地理空间索引
    /// <para xml:lang="en">2D Geospatial Index</para>
    /// </summary>
    Geo2D = 2,

    /// <summary>
    /// 2D 球面地理空间索引
    /// <para xml:lang="en">2D Sphere Geospatial Index</para>
    /// </summary>
    Geo2DSphere = 3,

    /// <summary>
    /// 哈希索引
    /// <para xml:lang="en">Hashed Index</para>
    /// </summary>
    Hashed = 4,

    /// <summary>
    /// 文本索引
    /// <para xml:lang="en">Text Index</para>
    /// </summary>
    Text = 5
}