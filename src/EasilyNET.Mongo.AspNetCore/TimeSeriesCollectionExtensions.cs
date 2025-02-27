using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using Microsoft.AspNetCore.Builder;
using MongoDB.Bson;
using MongoDB.Driver;

// ReSharper disable CheckNamespace
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Time series collection extension class</para>
///     <para xml:lang="zh">时间序列集合扩展类</para>
/// </summary>
public static class TimeSeriesCollectionExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Do not attempt to create a time series collection or view named system.profile. If you attempt to do so, MongoDB 6.3 and
    ///     later will return an IllegalOperation error. Earlier MongoDB versions will crash as a result.
    ///     </para>
    ///     <para xml:lang="zh">不要尝试创建名称为 system.profile 的时间序列集合或视图。如果您尝试这样做，MongoDB 6.3 及更高版本会返回 IllegalOperation 错误。早期 MongoDB 版本会因此崩溃。</para>
    /// </summary>
    private const string IllegalName = "system.profile";

    private static readonly ConcurrentBag<string> CollectionCache = [];

    /// <summary>
    ///     <para xml:lang="en">
    ///     Automatically create MongoDB time series collections for entity objects marked with
    ///     <see cref="TimeSeriesCollectionAttribute" />
    ///     </para>
    ///     <para xml:lang="zh">对标记 <see cref="TimeSeriesCollectionAttribute" /> 的实体对象,自动创建 MongoDB 时序集合</para>
    /// </summary>
    /// <param name="app">
    ///     <see cref="IApplicationBuilder" />
    /// </param>
    public static IApplicationBuilder UseCreateMongoTimeSeriesCollection<T>(this IApplicationBuilder app) where T : MongoContext
    {
        ArgumentNullException.ThrowIfNull(app);
        var db = app.ApplicationServices.GetService<T>();
        ArgumentNullException.ThrowIfNull(db, nameof(T));
        var collections = db.Database.ListCollectionNames().ToList();
        CollectionCache.AddRange([.. collections]);
        EnsureTimeSeriesCollections(db.Database);
        return app;
    }

    private static void EnsureTimeSeriesCollections(IMongoDatabase db)
    {
        var types = AssemblyHelper.FindTypesByAttribute<TimeSeriesCollectionAttribute>(o => o is { IsClass: true, IsAbstract: false });
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).First();
            var collectionName = attribute.CollectionName;
            if (IllegalName.Equals(collectionName, StringComparison.OrdinalIgnoreCase)) continue;
            var hasCache = CollectionCache.Contains(collectionName);
            // 如果缓存中存在且为true，跳过创建
            if (hasCache) continue;
            // 如果缓存中没有则创建集合
            db.CreateCollection(collectionName, new()
            {
                TimeSeriesOptions = attribute.TimeSeriesOptions,
                ExpireAfter = attribute.ExpireAfter
            });
            var collection = db.GetCollection<BsonDocument>(collectionName);
            var indexKeys = Builders<BsonDocument>.IndexKeys.Ascending(attribute.TimeSeriesOptions.TimeField);
            var indexOptions = new CreateIndexOptions { Background = true };
            collection.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexKeys, indexOptions));
            // 更新缓存
            CollectionCache.Add(collectionName);
        }
    }
}