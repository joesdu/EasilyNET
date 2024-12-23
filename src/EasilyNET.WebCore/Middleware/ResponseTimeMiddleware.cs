using System.Diagnostics;
using Microsoft.AspNetCore.Http;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Middleware;

/// <summary>
///     <para xml:lang="en">
///     API response time monitoring middleware. It should be placed as early as possible in the pipeline to reflect the time taken
///     by all middleware in the pipeline. The later it is placed, the more it reflects the execution time of the action. It can be flexibly configured
///     according to the actual situation.
///     </para>
///     <para xml:lang="zh">API 耗时监控中间件，应尽量靠前，越靠前越能体现整个管道中所有管道的耗时，越靠后越能体现 Action 的执行时间。可根据实际情况灵活配置位置。</para>
/// </summary>
internal sealed class ResponseTimeMiddleware(RequestDelegate next)
{
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