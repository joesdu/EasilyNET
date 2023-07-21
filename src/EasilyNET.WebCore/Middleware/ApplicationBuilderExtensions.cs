using Microsoft.AspNetCore.Builder;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// 中间件扩展,用于统一处理中间件调用扩展
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// 使用全局API耗时监控中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseResponseTime(this IApplicationBuilder builder) => builder.UseMiddleware<ResponseTimeMiddleware>();

    /// <summary>
    /// 使用全局异常中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IApplicationBuilder UseErrorHandling(this IApplicationBuilder builder) => builder.UseMiddleware<ErrorHandlingMiddleware>();

    /// <summary>
    /// 使用防抖中间件
    /// </summary>
    /// <param name="builder"></param>
    /// <example>
    ///     <code>
    ///       <![CDATA[
    ///        [RepeatSubmit(500), HttpGet]
    ///        public void Get() => "Hello World";
    ///       ]]>
    ///     </code>
    /// </example>
    /// <returns></returns>
    public static IApplicationBuilder UseRepeatSubmit(this IApplicationBuilder builder) => builder.UseMiddleware<RepeatSubmitMiddleware>();
}