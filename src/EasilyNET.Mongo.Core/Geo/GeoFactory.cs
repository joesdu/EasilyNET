using MongoDB.Driver.GeoJsonObjectModel;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core.Geo;

/// <summary>
///     <para xml:lang="en">
///     Factory methods for creating GeoJSON objects with simplified syntax.
///     Wraps the verbose MongoDB driver GeoJSON types for convenience.
///     </para>
///     <para xml:lang="zh">
///     使用简化语法创建 GeoJSON 对象的工厂方法。
///     封装冗长的 MongoDB 驱动 GeoJSON 类型以提供便利。
///     </para>
/// </summary>
public static class GeoPoint
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a GeoJSON Point from longitude and latitude.
    ///     Longitude must be between -180 and 180. Latitude must be between -90 and 90.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     从经度和纬度创建 GeoJSON Point。
    ///     经度必须在 -180 到 180 之间。纬度必须在 -90 到 90 之间。
    ///     </para>
    /// </summary>
    /// <param name="longitude">
    ///     <para xml:lang="en">Longitude (-180 to 180)</para>
    ///     <para xml:lang="zh">经度（-180 到 180）</para>
    /// </param>
    /// <param name="latitude">
    ///     <para xml:lang="en">Latitude (-90 to 90)</para>
    ///     <para xml:lang="zh">纬度（-90 到 90）</para>
    /// </param>
    /// <returns>
    ///     <see cref="GeoJsonPoint{TCoordinates}" />
    /// </returns>
    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> From(double longitude, double latitude) => new(new(longitude, latitude));

    /// <summary>
    ///     <para xml:lang="en">Create a GeoJSON Point from a coordinate tuple (longitude, latitude)</para>
    ///     <para xml:lang="zh">从坐标元组（经度，纬度）创建 GeoJSON Point</para>
    /// </summary>
    /// <param name="coordinates">
    ///     <para xml:lang="en">A tuple of (longitude, latitude)</para>
    ///     <para xml:lang="zh">（经度，纬度）元组</para>
    /// </param>
    public static GeoJsonPoint<GeoJson2DGeographicCoordinates> From((double Longitude, double Latitude) coordinates) => new(new(coordinates.Longitude, coordinates.Latitude));
}

/// <summary>
///     <para xml:lang="en">
///     Factory methods for creating GeoJSON Polygon objects with simplified syntax.
///     </para>
///     <para xml:lang="zh">
///     使用简化语法创建 GeoJSON Polygon 对象的工厂方法。
///     </para>
/// </summary>
public static class GeoPolygon
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a GeoJSON Polygon from an array of coordinate tuples.
    ///     The first and last coordinates must be the same to close the polygon.
    ///     A polygon must have at least 4 coordinate pairs (3 unique points + closing point).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     从坐标元组数组创建 GeoJSON Polygon。
    ///     第一个和最后一个坐标必须相同以闭合多边形。
    ///     多边形必须至少有 4 个坐标对（3 个唯一点 + 闭合点）。
    ///     </para>
    /// </summary>
    /// <param name="coordinates">
    ///     <para xml:lang="en">Array of (longitude, latitude) tuples forming the polygon ring</para>
    ///     <para xml:lang="zh">构成多边形环的（经度，纬度）元组数组</para>
    /// </param>
    public static GeoJsonPolygon<GeoJson2DGeographicCoordinates> From(params (double Longitude, double Latitude)[] coordinates)
    {
        if (coordinates.Length < 4)
        {
            throw new ArgumentException("A polygon must have at least 4 coordinate pairs (3 unique points + closing point).", nameof(coordinates));
        }
        if (coordinates[0] != coordinates[^1])
        {
            throw new ArgumentException("The polygon must be closed: the first and last coordinates must be the same.", nameof(coordinates));
        }
        var positions = coordinates.Select(c => new GeoJson2DGeographicCoordinates(c.Longitude, c.Latitude)).ToArray();
        return new(new(new(positions)));
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a GeoJSON Polygon from an array of <see cref="GeoJson2DGeographicCoordinates" />.
    ///     The first and last coordinates must be the same to close the polygon.
    ///     A polygon must have at least 4 coordinate pairs (3 unique points + closing point).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     从 <see cref="GeoJson2DGeographicCoordinates" /> 数组创建 GeoJSON Polygon。
    ///     第一个和最后一个坐标必须相同以闭合多边形。
    ///     多边形必须至少有 4 个坐标对（3 个唯一点 + 闭合点）。
    ///     </para>
    /// </summary>
    /// <param name="coordinates">
    ///     <para xml:lang="en">Array of geographic coordinates forming the polygon ring</para>
    ///     <para xml:lang="zh">构成多边形环的地理坐标数组</para>
    /// </param>
    public static GeoJsonPolygon<GeoJson2DGeographicCoordinates> From(params GeoJson2DGeographicCoordinates[] coordinates) =>
        coordinates.Length < 4
            ? throw new ArgumentException("A polygon must have at least 4 coordinate pairs (3 unique points + closing point).", nameof(coordinates))
            : !coordinates[0].Equals(coordinates[^1])
                ? throw new ArgumentException("The polygon must be closed: the first and last coordinates must be the same.", nameof(coordinates))
                : new(new(new(coordinates)));
}