using EasilyNET.Mongo.AspNetCore.SearchIndex;
using EasilyNET.Mongo.Core;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for automatically creating MongoDB Atlas Search and Vector Search indexes</para>
///     <para xml:lang="zh">自动创建 MongoDB Atlas Search 和 Vector Search 索引的扩展方法</para>
/// </summary>
public static class SearchIndexExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Register a hosted background service that automatically creates MongoDB Atlas Search and Vector Search indexes
    ///     for entity objects marked with <c>MongoSearchIndexAttribute</c>.
    ///     The service runs once at application startup and then completes.
    ///     Requires MongoDB Atlas or MongoDB 8.2+ Community Edition.
    ///     On unsupported deployments, the service logs a warning and skips index creation.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     注册一个托管后台服务，自动为标记了 <c>MongoSearchIndexAttribute</c> 的实体对象创建 MongoDB Atlas Search 和 Vector Search 索引。
    ///     该服务在应用启动时运行一次后完成。
    ///     需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。
    ///     在不支持的部署上，该服务记录警告并跳过索引创建。
    ///     </para>
    /// </summary>
    /// <typeparam name="T">
    ///     <see cref="MongoContext" />
    /// </typeparam>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    /// <returns>
    ///     <see cref="IServiceCollection" />
    /// </returns>
    public static IServiceCollection AddMongoSearchIndexCreation<T>(this IServiceCollection services) where T : MongoContext
    {
        services.AddHostedService<SearchIndexBackgroundService<T>>();
        return services;
    }
}