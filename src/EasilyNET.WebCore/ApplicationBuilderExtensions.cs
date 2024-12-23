using EasilyNET.WebCore.Middleware;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.AspNetCore.Builder;

/// <summary>
///     <para xml:lang="en">Middleware extensions for unified handling of middleware invocation extensions</para>
///     <para xml:lang="zh">中间件扩展，用于统一处理中间件调用扩展</para>
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Uses the global API response time monitoring middleware</para>
    ///     <para xml:lang="zh">使用全局 API 耗时监控中间件</para>
    /// </summary>
    public static IApplicationBuilder UseResponseTime(this IApplicationBuilder builder) => builder.UseMiddleware<ResponseTimeMiddleware>();
}