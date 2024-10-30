using System.Diagnostics;
using Microsoft.AspNetCore.Http;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// API耗时监控中间件,应尽量靠前,越靠前越能体现整个管道中所有管道的耗时,越靠后越能体现Action的执行时间.可根据实际情况灵活配置位置.
/// </summary>
/// <remarks>
/// 构造函数
/// </remarks>
/// <param name="next"></param>
internal sealed class ResponseTimeMiddleware(RequestDelegate next)
{
    /// <summary>
    /// Invoke
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var start = Stopwatch.GetTimestamp();
        context.Response.OnStarting(() =>
        {
            var end = Stopwatch.GetTimestamp();
            var elapsedMilliseconds = Stopwatch.GetElapsedTime(start, end).TotalMilliseconds;
            context.Response.Headers["X-Response-Time"] = $"{elapsedMilliseconds} ms";
            return Task.CompletedTask;
        });
        await next(context);
    }
}