using EasilyNET.Mongo.AspNetCore.Options;
using EasilyNET.Mongo.Core;
using MongoDB.Driver.GridFS;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for registering GridFS bucket in the DI container</para>
///     <para xml:lang="zh">在 DI 容器中注册 GridFS 存储桶的扩展方法</para>
/// </summary>
public static class GridFSServiceExtensions
{
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a <see cref="GridFSBucket" /> as a singleton in the DI container.
        ///     The <see cref="MongoContext" /> subclass <typeparamref name="TContext" /> must already be registered (via <c>AddMongoContext</c>).
        ///     The <c>IMongoDatabase</c> is obtained from the context's <see cref="MongoContext.Database" /> property.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     在 DI 容器中将 <see cref="GridFSBucket" /> 注册为单例。
        ///     <see cref="MongoContext" /> 子类 <typeparamref name="TContext" /> 必须已经注册（通过 <c>AddMongoContext</c>）。
        ///     <c>IMongoDatabase</c> 从上下文的 <see cref="MongoContext.Database" /> 属性获取。
        ///     </para>
        ///     <para xml:lang="en">
        ///     <b>Note:</b> If you need multiple GridFS buckets, use the keyed overload
        ///     <c>AddGridFSBucket&lt;TContext&gt;(serviceKey, databaseName, configure)</c> instead.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     <b>注意：</b>如果需要多个 GridFS 存储桶，请使用键控重载
        ///     <c>AddGridFSBucket&lt;TContext&gt;(serviceKey, databaseName, configure)</c>。
        ///     </para>
        /// </summary>
        /// <typeparam name="TContext">
        ///     <para xml:lang="en">The <see cref="MongoContext" /> subclass to resolve from DI.</para>
        ///     <para xml:lang="zh">从 DI 解析的 <see cref="MongoContext" /> 子类。</para>
        /// </typeparam>
        /// <param name="configure">
        ///     <para xml:lang="en">Optional configuration for the GridFS bucket.</para>
        ///     <para xml:lang="zh">GridFS 存储桶的可选配置。</para>
        /// </param>
        /// <returns>
        ///     <see cref="IServiceCollection" />
        /// </returns>
        public IServiceCollection AddGridFSBucket<TContext>(Action<GridFSOptions>? configure = null) where TContext : MongoContext
        {
            var options = new GridFSOptions();
            configure?.Invoke(options);
            services.AddSingleton<IGridFSBucket>(sp =>
            {
                var context = sp.GetRequiredService<TContext>();
                return new GridFSBucket(context.Database, options.ToGridFSBucketOptions());
            });
            return services;
        }

        /// <summary>
        ///     <para xml:lang="en">
        ///     Register a keyed <see cref="GridFSBucket" /> using a specific database from the <see cref="MongoContext" /> subclass's client.
        ///     Useful when you need GridFS on a different database than the default one.
        ///     Use <c>[FromKeyedServices(serviceKey)]</c> to inject in constructors.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     使用 <see cref="MongoContext" /> 子类的客户端中的特定数据库注册键控的 <see cref="GridFSBucket" />。
        ///     当需要在非默认数据库上使用 GridFS 时有用。
        ///     在构造函数中使用 <c>[FromKeyedServices(serviceKey)]</c> 特性注入。
        ///     </para>
        /// </summary>
        /// <typeparam name="TContext">
        ///     <para xml:lang="en">The <see cref="MongoContext" /> subclass to resolve from DI.</para>
        ///     <para xml:lang="zh">从 DI 解析的 <see cref="MongoContext" /> 子类。</para>
        /// </typeparam>
        /// <param name="serviceKey">
        ///     <para xml:lang="en">The key used to identify this GridFS bucket in DI container.</para>
        ///     <para xml:lang="zh">用于在 DI 容器中标识此 GridFS 存储桶的键。</para>
        /// </param>
        /// <param name="databaseName">
        ///     <para xml:lang="en">The name of the database to use for GridFS.</para>
        ///     <para xml:lang="zh">用于 GridFS 的数据库名称。</para>
        /// </param>
        /// <param name="configure">
        ///     <para xml:lang="en">Optional configuration for the GridFS bucket.</para>
        ///     <para xml:lang="zh">GridFS 存储桶的可选配置。</para>
        /// </param>
        /// <returns>
        ///     <see cref="IServiceCollection" />
        /// </returns>
        /// <example>
        ///     <para xml:lang="en">Registration:</para>
        ///     <code>
        /// services.AddGridFSBucket&lt;MyDbContext&gt;("media", "media-db");
        /// services.AddGridFSBucket&lt;MyDbContext&gt;("documents", "docs-db");
        ///     </code>
        ///     <para xml:lang="en">Injection:</para>
        ///     <code>
        /// public class MediaService([FromKeyedServices("media")] IGridFSBucket mediaBucket)
        ///     </code>
        /// </example>
        public IServiceCollection AddGridFSBucket<TContext>(string serviceKey, string databaseName, Action<GridFSOptions>? configure = null) where TContext : MongoContext
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(serviceKey);
            ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
            var options = new GridFSOptions();
            configure?.Invoke(options);
            services.AddKeyedSingleton<IGridFSBucket>(serviceKey, (sp, _) =>
            {
                var context = sp.GetRequiredService<TContext>();
                var database = context.Client.GetDatabase(databaseName);
                return new GridFSBucket(database, options.ToGridFSBucketOptions());
            });
            return services;
        }
    }
}