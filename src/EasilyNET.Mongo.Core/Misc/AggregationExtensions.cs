using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.Core.Misc;

/// <summary>
///     <para xml:lang="en">
///     Extension methods providing convenient aggregation pipeline operations on <see cref="IMongoCollection{TDocument}" />.
///     These are shortcuts for commonly used aggregation patterns.
///     </para>
///     <para xml:lang="zh">
///     在 <see cref="IMongoCollection{TDocument}" /> 上提供便捷聚合管道操作的扩展方法。
///     这些是常用聚合模式的快捷方式。
///     </para>
/// </summary>
public static class AggregationExtensions
{
    private static string ResolveFieldName<TDocument>(Expression<Func<TDocument, object?>> field)
    {
        var expression = field.Body;
        if (expression is UnaryExpression { NodeType: ExpressionType.Convert } unary)
        {
            expression = unary.Operand;
        }
        if (expression is MemberExpression member)
        {
            // Try to get the BSON element name from the class map (respects conventions and [BsonElement])
            var classMap = BsonClassMap.LookupClassMap(typeof(TDocument));
            var memberMap = classMap?.GetMemberMap(member.Member.Name);
            return memberMap?.ElementName ?? member.Member.Name;
        }
        return string.Empty;
    }

    extension<TDocument>(IMongoCollection<TDocument> collection)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute a <c>$lookup</c> + <c>$unwind</c> pipeline to perform a left join with another collection
        ///     and flatten the result. This is the most common join pattern in MongoDB.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     执行 <c>$lookup</c> + <c>$unwind</c> 管道以与另一个集合执行左连接并展平结果。
        ///     这是 MongoDB 中最常见的连接模式。
        ///     </para>
        /// </summary>
        public async Task<List<BsonDocument>> LookupAndUnwindAsync<TForeign>(
            string foreignCollectionName,
            Expression<Func<TDocument, object?>> localField,
            Expression<Func<TForeign, object?>> foreignField,
            string asField,
            FilterDefinition<TDocument>? filter = null,
            bool preserveNullAndEmpty = true,
            CancellationToken cancellationToken = default)
        {
            var localFieldName = ResolveFieldName(localField);
            var foreignFieldName = ResolveFieldName(foreignField);
            var pipeline = new List<BsonDocument>();
            if (filter is not null)
            {
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var documentSerializer = serializerRegistry.GetSerializer<TDocument>();
                pipeline.Add(new("$match", filter.Render(new(documentSerializer, serializerRegistry))));
            }
            pipeline.Add(new("$lookup", new BsonDocument
            {
                { "from", foreignCollectionName },
                { "localField", localFieldName },
                { "foreignField", foreignFieldName },
                { "as", asField }
            }));
            var unwindDoc = new BsonDocument("path", $"${asField}");
            if (preserveNullAndEmpty)
            {
                unwindDoc.Add("preserveNullAndEmptyArrays", true);
            }
            pipeline.Add(new("$unwind", unwindDoc));
            var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            return await bsonCollection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute a <c>$group</c> pipeline to count documents by a grouping field.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     执行 <c>$group</c> 管道以按分组字段统计文档数量。
        ///     </para>
        /// </summary>
        public async Task<Dictionary<string, long>> GroupByCountAsync(
            Expression<Func<TDocument, object?>> groupByField,
            FilterDefinition<TDocument>? filter = null,
            CancellationToken cancellationToken = default)
        {
            var fieldName = ResolveFieldName(groupByField);
            var pipeline = new List<BsonDocument>();
            if (filter is not null)
            {
                var serializerRegistry = BsonSerializer.SerializerRegistry;
                var documentSerializer = serializerRegistry.GetSerializer<TDocument>();
                pipeline.Add(new("$match", filter.Render(new(documentSerializer, serializerRegistry))));
            }
            pipeline.Add(new("$group", new BsonDocument
            {
                { "_id", $"${fieldName}" },
                { "count", new BsonDocument("$sum", 1) }
            }));
            pipeline.Add(new("$sort", new BsonDocument("count", -1)));
            var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            var results = await bsonCollection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
            var dict = new Dictionary<string, long>();
            foreach (var doc in results)
            {
                var key = doc["_id"].IsBsonNull ? "(null)" : doc["_id"].ToString()!;
                dict[key] = doc["count"].AsInt64;
            }
            return dict;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute a <c>$bucket</c> pipeline to categorize documents into groups based on a specified field and boundaries.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     执行 <c>$bucket</c> 管道以根据指定字段和边界将文档分类到组中。
        ///     </para>
        /// </summary>
        public async Task<List<BsonDocument>> BucketAsync(
            Expression<Func<TDocument, object?>> groupByField,
            BsonValue[] boundaries,
            string? defaultBucket = "Other",
            CancellationToken cancellationToken = default)
        {
            var fieldName = ResolveFieldName(groupByField);
            var bucketStage = new BsonDocument
            {
                { "groupBy", $"${fieldName}" },
                { "boundaries", new BsonArray(boundaries) },
                { "output", new BsonDocument("count", new BsonDocument("$sum", 1)) }
            };
            if (defaultBucket is not null)
            {
                bucketStage.Add("default", defaultBucket);
            }
            var pipeline = new List<BsonDocument> { new("$bucket", bucketStage) };
            var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            return await bsonCollection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Execute a <c>$facet</c> pipeline to run multiple aggregation pipelines in parallel
        ///     and return their results in a single document.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     执行 <c>$facet</c> 管道以并行运行多个聚合管道并在单个文档中返回其结果。
        ///     </para>
        /// </summary>
        public async Task<BsonDocument> FacetAsync(Dictionary<string, BsonDocument[]> facets, CancellationToken cancellationToken = default)
        {
            var facetDoc = new BsonDocument();
            foreach (var (name, stages) in facets)
            {
                facetDoc.Add(name, new BsonArray(stages));
            }
            var pipeline = new List<BsonDocument> { new("$facet", facetDoc) };
            var bsonCollection = collection.Database.GetCollection<BsonDocument>(collection.CollectionNamespace.CollectionName);
            var results = await bsonCollection.Aggregate<BsonDocument>(pipeline).ToListAsync(cancellationToken).ConfigureAwait(false);
            return results.Count > 0 ? results[0] : [];
        }
    }
}