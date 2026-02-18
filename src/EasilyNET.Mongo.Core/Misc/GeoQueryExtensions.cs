using System.Linq.Expressions;
using EasilyNET.Mongo.Core.Geo;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core.Misc;

/// <summary>
///     <para xml:lang="en">Extension methods for geospatial queries on <see cref="IMongoCollection{TDocument}" /></para>
///     <para xml:lang="zh"><see cref="IMongoCollection{TDocument}" /> 上的地理空间查询扩展方法</para>
/// </summary>
public static class GeoQueryExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a <c>$nearSphere</c> filter for finding documents near a given point.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     创建 <c>$nearSphere</c> 过滤器以查找给定点附近的文档。
    ///     </para>
    /// </summary>
    public static FilterDefinition<TDocument> NearSphere<TDocument>(
        Expression<Func<TDocument, object?>> field,
        GeoJsonPoint<GeoJson2DGeographicCoordinates> near,
        double? maxDistanceMeters = null,
        double? minDistanceMeters = null) =>
        Builders<TDocument>.Filter.NearSphere(field, near, maxDistanceMeters, minDistanceMeters);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a <c>$geoWithin</c> filter for finding documents within a polygon.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     创建 <c>$geoWithin</c> 过滤器以查找多边形区域内的文档。
    ///     </para>
    /// </summary>
    public static FilterDefinition<TDocument> GeoWithin<TDocument>(
        Expression<Func<TDocument, object?>> field,
        GeoJsonPolygon<GeoJson2DGeographicCoordinates> polygon) =>
        Builders<TDocument>.Filter.GeoWithin(field, polygon);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a <c>$geoIntersects</c> filter for finding documents that intersect with a geometry.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     创建 <c>$geoIntersects</c> 过滤器以查找与几何图形相交的文档。
    ///     </para>
    /// </summary>
    public static FilterDefinition<TDocument> GeoIntersects<TDocument>(
        Expression<Func<TDocument, object?>> field,
        GeoJsonGeometry<GeoJson2DGeographicCoordinates> geometry) =>
        Builders<TDocument>.Filter.GeoIntersects(field, geometry);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Create a <c>$nearSphere</c> filter using simplified coordinate parameters.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     使用简化的坐标参数创建 <c>$nearSphere</c> 过滤器。
    ///     </para>
    /// </summary>
    public static FilterDefinition<TDocument> NearSphere<TDocument>(
        Expression<Func<TDocument, object?>> field,
        double longitude,
        double latitude,
        double? maxDistanceMeters = null,
        double? minDistanceMeters = null) =>
        NearSphere(field, GeoPoint.From(longitude, latitude), maxDistanceMeters, minDistanceMeters);

    private static string ResolveFieldName<TDocument>(Expression<Func<TDocument, object?>> field)
    {
        var expression = field.Body;
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            expression = unary.Operand;
        }
        return expression is MemberExpression member ? member.Member.Name : string.Empty;
    }

    extension<TDocument>(IMongoCollection<TDocument> collection)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute a <c>$geoNear</c> aggregation pipeline to find documents near a given point,
        ///     sorted by distance. The collection must have a <c>2dsphere</c> index on the specified field.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     执行 <c>$geoNear</c> 聚合管道以查找给定点附近的文档，按距离排序。
        ///     集合必须在指定字段上有 <c>2dsphere</c> 索引。
        ///     </para>
        /// </summary>
        /// <param name="field">
        ///     <para xml:lang="en">Expression selecting the GeoJSON field</para>
        ///     <para xml:lang="zh">选择 GeoJSON 字段的表达式</para>
        /// </param>
        /// <param name="near">
        ///     <para xml:lang="en">The point to search near</para>
        ///     <para xml:lang="zh">要搜索附近的点</para>
        /// </param>
        /// <param name="options">
        ///     <para xml:lang="en">GeoNear options</para>
        ///     <para xml:lang="zh">GeoNear 选项</para>
        /// </param>
        /// <param name="cancellationToken">
        ///     <para xml:lang="en">Cancellation token</para>
        ///     <para xml:lang="zh">取消令牌</para>
        /// </param>
        /// <returns>
        ///     <para xml:lang="en">List of documents with distance information</para>
        ///     <para xml:lang="zh">包含距离信息的文档列表</para>
        /// </returns>
        public async Task<List<BsonDocument>> GeoNearAsync(
            Expression<Func<TDocument, object?>> field,
            GeoJsonPoint<GeoJson2DGeographicCoordinates> near,
            GeoNearOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(collection);
            ArgumentNullException.ThrowIfNull(field);
            ArgumentNullException.ThrowIfNull(near);
            options ??= new();
            var geoNearStage = new BsonDocument("$geoNear", new BsonDocument
            {
                { "near", new BsonDocument { { "type", "Point" }, { "coordinates", new BsonArray { near.Coordinates.Longitude, near.Coordinates.Latitude } } } },
                { "distanceField", options.DistanceField },
                { "spherical", options.Spherical }
            });
            if (options.MaxDistanceMeters.HasValue)
            {
                geoNearStage["$geoNear"]["maxDistance"] = options.MaxDistanceMeters.Value;
            }
            if (options.MinDistanceMeters.HasValue)
            {
                geoNearStage["$geoNear"]["minDistance"] = options.MinDistanceMeters.Value;
            }
            if (options.Filter is not null)
            {
                geoNearStage["$geoNear"]["query"] = options.Filter;
            }
            // Resolve the field name from the expression
            var fieldName = ResolveFieldName(field);
            if (!string.IsNullOrEmpty(fieldName))
            {
                geoNearStage["$geoNear"]["key"] = fieldName;
            }
            var pipeline = new List<BsonDocument> { geoNearStage };
            if (options.Limit.HasValue)
            {
                pipeline.Add(new("$limit", options.Limit.Value));
            }
            var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            return await bsonCollection.Aggregate<BsonDocument>(pipeline, cancellationToken: cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}