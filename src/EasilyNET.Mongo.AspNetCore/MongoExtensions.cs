using System.Collections.Concurrent;
using System.Reflection;
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
    private static readonly ConcurrentDictionary<string, bool> CollectionCache = new();

    /// <summary>
    /// 对标记TimeSeriesCollectionAttribute创建MongoDB的时序集合
    /// </summary>
    /// <param name="app"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseCreateMongoTimeSeriesCollection(this IApplicationBuilder app)
    {
        var mongo = app.ApplicationServices.GetRequiredService<MongoContext>();
        EnsureTimeSeriesCollections(mongo.Database);
        return app;
    }

    private static void EnsureTimeSeriesCollections(IMongoDatabase database)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var typesWithAttribute = assembly.GetTypes()
                                         .Where(t => t.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).Any());
        var collectionList = database.ListCollectionNames().ToList();
        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).First();
            var collectionName = type.Name;
            CollectionCache.TryGetValue(collectionName, out var value);
            // 如果缓存中存在且为true，跳过创建
            if (value) continue;
            // 如果缓存中没有则添加
            CollectionCache[collectionName] = collectionList.Contains(collectionName);
            database.CreateCollection(collectionName, new()
            {
                TimeSeriesOptions = attribute.TimeSeriesOptions,
                ExpireAfter = attribute.ExpireAfter
            });
            // 更新缓存
            CollectionCache[collectionName] = true;
        }
    }
}