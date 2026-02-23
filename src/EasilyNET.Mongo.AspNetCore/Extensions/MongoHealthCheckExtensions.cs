using EasilyNET.Mongo.AspNetCore.HealthChecks;
using EasilyNET.Mongo.Core;
using Microsoft.Extensions.Diagnostics.HealthChecks;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for adding MongoDB health checks</para>
///     <para xml:lang="zh">添加 MongoDB 健康检查的扩展方法</para>
/// </summary>
public static class MongoHealthCheckExtensions
{
    extension(IHealthChecksBuilder builder)
    {
        /// <summary>
        ///     <para xml:lang="en">
        ///     Add a health check for MongoDB. Verifies connectivity by executing a <c>ping</c> command against the database.
        ///     The <see cref="MongoContext" /> subclass <typeparamref name="TContext" /> is resolved from the service provider
        ///     to obtain the <c>IMongoClient</c> and database name.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     添加 MongoDB 健康检查。通过对数据库执行 <c>ping</c> 命令来验证连接性。
        ///     从服务提供程序中解析 <see cref="MongoContext" /> 子类 <typeparamref name="TContext" /> 以获取 <c>IMongoClient</c> 和数据库名称。
        ///     </para>
        /// </summary>
        /// <typeparam name="TContext">
        ///     <para xml:lang="en">The <see cref="MongoContext" /> subclass to resolve from DI.</para>
        ///     <para xml:lang="zh">从 DI 解析的 <see cref="MongoContext" /> 子类。</para>
        /// </typeparam>
        /// <param name="name">
        ///     <para xml:lang="en">The health check name. Defaults to <c>"mongodb"</c>.</para>
        ///     <para xml:lang="zh">健康检查名称。默认为 <c>"mongodb"</c>。</para>
        /// </param>
        /// <param name="failureStatus">
        ///     <para xml:lang="en">
        ///     The <see cref="HealthStatus" /> that should be reported when the health check reports a failure.
        ///     Defaults to <see cref="HealthStatus.Unhealthy" />.
        ///     </para>
        ///     <para xml:lang="zh">健康检查报告失败时应报告的 <see cref="HealthStatus" />。默认为 <see cref="HealthStatus.Unhealthy" />。</para>
        /// </param>
        /// <param name="tags">
        ///     <para xml:lang="en">Optional tags for filtering health checks.</para>
        ///     <para xml:lang="zh">用于筛选健康检查的可选标签。</para>
        /// </param>
        /// <param name="timeout">
        ///     <para xml:lang="en">Optional timeout for the health check. Defaults to <c>null</c> (no timeout).</para>
        ///     <para xml:lang="zh">健康检查的可选超时时间。默认为 <c>null</c>（无超时）。</para>
        /// </param>
        /// <returns>
        ///     <see cref="IHealthChecksBuilder" />
        /// </returns>
        public IHealthChecksBuilder AddMongoHealthCheck<TContext>(string name = "mongodb", HealthStatus? failureStatus = null, IEnumerable<string>? tags = null, TimeSpan? timeout = null) where TContext : MongoContext
        {
            return builder.Add(new(name, sp =>
            {
                var context = sp.GetRequiredService<TContext>();
                return new MongoHealthCheck(context.Client, context.Database.DatabaseNamespace.DatabaseName);
            }, failureStatus, tags, timeout));
        }
    }
}