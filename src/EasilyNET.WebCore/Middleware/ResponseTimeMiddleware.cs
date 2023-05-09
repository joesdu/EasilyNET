using Microsoft.AspNetCore.Http;
using System.Diagnostics;

// ReSharper disable ClassNeverInstantiated.Global

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// API耗时监控中间件,应尽量靠前,越靠前越能体现整个管道中所有管道的耗时,越靠后越能体现Action的执行时间.可根据实际情况灵活配置位置.
/// </summary>
public sealed class ResponseTimeMiddleware
{
    private const string ResponseTime = "X-Response-Time";
    private readonly RequestDelegate _next;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="next"></param>
    public ResponseTimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>
    /// Invoke
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task Invoke(HttpContext context)
    {
        var watch = new Stopwatch();
        watch.Start();
        context.Response.OnStarting(() =>
        {
            watch.Stop();
            context.Response.Headers[ResponseTime] = $"{watch.ElapsedMilliseconds} ms";
            return Task.CompletedTask;
        });
        await _next(context);
    }
}