using EasilyNET.Mongo.AspNetCore.Indexing;
using EasilyNET.Mongo.Core;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for automatically creating MongoDB indexes</para>
///     <para xml:lang="zh">自动创建 MongoDB 索引的扩展方法</para>
/// </summary>
public static class CollectionIndexExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Register a hosted background service that automatically creates MongoDB indexes for entity objects marked with
    ///     <c>MongoIndexAttribute</c> / <c>MongoCompoundIndexAttribute</c>. The service runs once at application startup
    ///     (without blocking startup, using the async driver APIs) and then completes.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     注册一个托管后台服务，自动为标记了 <c>MongoIndexAttribute</c> / <c>MongoCompoundIndexAttribute</c> 的实体对象创建 MongoDB 索引。
    ///     该服务在应用启动时运行一次（不阻塞启动，全程使用异步驱动 API），完成后结束。
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
    public static IServiceCollection AddMongoIndexCreation<T>(this IServiceCollection services) where T : MongoContext
    {
        services.AddHostedService<IndexCreationBackgroundService<T>>();
        return services;
    }
}
