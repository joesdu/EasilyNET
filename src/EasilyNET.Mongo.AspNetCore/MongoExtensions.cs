using System.Collections.Concurrent;
using System.Reflection;
using EasilyNET.Core.Misc;
using EasilyNET.Mongo.Core;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Mongo.AspNetCore;

/// <summary>
/// MongoExtensions
/// </summary>
public static class MongoExtensions
{
    /// <summary>
    /// 不要尝试创建名称为 system.profile 的时间序列集合或视图。如果您尝试这样做，MongoDB 6.3 及更高版本会返回 IllegalOperation 错误。早期 MongoDB 版本会因此崩溃。
    /// </summary>
    private const string IllegalName = "system.profile";

    private static readonly ConcurrentDictionary<string, bool> CollectionCache = new();

    /// <summary>
    /// 对标记TimeSeriesCollectionAttribute创建MongoDB的时序集合
    /// </summary>
    /// <param name="app"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseCreateMongoTimeSeriesCollection(this IApplicationBuilder app, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assemblies);
        var db = app.ApplicationServices.GetRequiredService<IMongoDatabase>();
        ArgumentNullException.ThrowIfNull(db, nameof(IMongoDatabase));
        var collections = db.ListCollectionNames().ToList();
        foreach (var item in collections)
        {
            CollectionCache.AddOrUpdate(item, true, (_, _) => true);
        }
        EnsureTimeSeriesCollections(db);
        return app;
    }

    private static void EnsureTimeSeriesCollections(IMongoDatabase db)
    {
        var types = AssemblyHelper.FindTypes(o => o is { IsClass: true, IsAbstract: false } && o.HasAttribute<TimeSeriesCollectionAttribute>());
        foreach (var type in types)
        {
            var attribute = type.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).First();
            var collectionName = attribute.CollectionName;
            if (IllegalName.Equals(collectionName, StringComparison.OrdinalIgnoreCase)) continue;
            CollectionCache.TryGetValue(collectionName, out var value);
            // 如果缓存中存在且为true，跳过创建
            if (value) continue;
            // 如果缓存中没有则添加
            CollectionCache.TryAdd(collectionName, false);
            db.CreateCollection(collectionName, new()
            {
                TimeSeriesOptions = attribute.TimeSeriesOptions,
                ExpireAfter = attribute.ExpireAfter
            });
            // 更新缓存
            CollectionCache[collectionName] = true;
        }
    }
}