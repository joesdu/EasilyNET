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
    /// 不要尝试创建名称为 system.profile 的时间序列集合或视图。如果您尝试这样做，MongoDB 6.3 及更高版本会返回 IllegalOperation 错误。早期 MongoDB 版本会因此崩溃。
    /// </summary>
    private const string IllegalName = "system.profile";

    /// <summary>
    /// 对标记TimeSeriesCollectionAttribute创建MongoDB的时序集合
    /// </summary>
    /// <param name="app"></param>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:正确实例化参数异常", Justification = "<挂起>")]
    public static IApplicationBuilder UseCreateMongoTimeSeriesCollection(this IApplicationBuilder app, IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(app);

        ArgumentNullException.ThrowIfNull(assemblies);

        var mongo = app.ApplicationServices.GetRequiredService<IMongoDatabase>();
        if (mongo == null)
        {
            throw new ArgumentNullException(nameof(IMongoDatabase));
        }

        EnsureTimeSeriesCollections(mongo, assemblies);
        return app;
    }

    private static void EnsureTimeSeriesCollections(IMongoDatabase database, IEnumerable<Assembly> assemblies)
    {
        var collectionList = database.ListCollectionNames().ToList();

        var typesWithAttribute = GetTypesWithAttribute<TimeSeriesCollectionAttribute>(assemblies);
        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttributes<TimeSeriesCollectionAttribute>(false).First();
            var collectionName = attribute.CollectionName;

            if (IllegalName.Equals(collectionName, StringComparison.OrdinalIgnoreCase))
                continue;

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

    /// <summary>
    /// 获取程序集中指定的特性
    /// </summary>
    /// <typeparam name="TAttribute"></typeparam>
    /// <param name="assemblies"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetTypesWithAttribute<TAttribute>(IEnumerable<Assembly> assemblies) where TAttribute : Attribute
    {
        return GetTypesWithAttribute(assemblies, typeof(TAttribute));
    }

    /// <summary>
    /// 获取程序集中指定的特性
    /// </summary>
    /// <param name="assemblies"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static IEnumerable<Type> GetTypesWithAttribute(IEnumerable<Assembly> assemblies, Type type)
    {
        foreach (var assembly in assemblies)
        {
            IEnumerable<Type>? types = null;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).Select(x => x!);
            }

            foreach (var item in types)
            {
                if (type.GetCustomAttributes(type, inherit: false).Length != 0)
                {
                    yield return type;
                }
            }
        }
    }

}