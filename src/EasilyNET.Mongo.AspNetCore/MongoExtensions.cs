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
    /// <summary>
    /// 不要尝试创建名称为 system.profile 的时间序列集合或视图。如果您尝试这样做，MongoDB 6.3 及更高版本会返回 IllegalOperation 错误。早期 MongoDB 版本会因此崩溃。
    /// </summary>
    const string IllegalName = "system.profile";

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
                                         .Where(t => t.GetCustomAttributes(typeof(TimeSeriesCollectionAttribute), false).Length > 0);
        foreach (var type in typesWithAttribute)
        {
            var attribute = (TimeSeriesCollectionAttribute)type.GetCustomAttributes(typeof(TimeSeriesCollectionAttribute), false).First();
            var collectionName = type.Name;
            if (IllegalName.Equals(collectionName.ToLowerInvariant()))
            {
                continue;
            }
            var collectionList = database.ListCollectionNames().ToList();
            if (!collectionList.Contains(collectionName))
            {
                var options = new CreateCollectionOptions
                {
                    TimeSeriesOptions = attribute.TimeSeriesOptions,
                    ExpireAfter = attribute.ExpireAfter
                };
                database.CreateCollection(collectionName, options);
            }
        }
    }
}