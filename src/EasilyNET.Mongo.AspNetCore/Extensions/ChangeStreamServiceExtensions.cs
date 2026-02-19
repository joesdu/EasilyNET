using EasilyNET.Mongo.AspNetCore.ChangeStreams;
using Microsoft.Extensions.Hosting;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
///     <para xml:lang="en">Extension methods for registering MongoDB Change Stream handlers</para>
///     <para xml:lang="zh">注册 MongoDB 变更流处理程序的扩展方法</para>
/// </summary>
public static class ChangeStreamServiceExtensions
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Register a <see cref="MongoChangeStreamHandler{TDocument}" /> implementation as a hosted background service.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     将 <see cref="MongoChangeStreamHandler{TDocument}" /> 实现注册为托管后台服务。
    ///     </para>
    ///     <example>
    ///         <code>
    ///     builder.Services.AddMongoChangeStreamHandler&lt;OrderChangeHandler&gt;();
    ///         </code>
    ///     </example>
    /// </summary>
    /// <typeparam name="THandler">
    ///     <para xml:lang="en">The change stream handler type</para>
    ///     <para xml:lang="zh">变更流处理程序类型</para>
    /// </typeparam>
    /// <param name="services">
    ///     <see cref="IServiceCollection" />
    /// </param>
    /// <returns>
    ///     <see cref="IServiceCollection" />
    /// </returns>
    public static IServiceCollection AddMongoChangeStreamHandler<THandler>(this IServiceCollection services)
        where THandler : class, IHostedService
    {
        services.AddHostedService<THandler>();
        return services;
    }
}