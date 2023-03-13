using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace EasilyNET.WebCore.Middleware;

/// <summary>
/// API耗时监控中间件,应尽量靠前,越靠前越能体现整个管道中所有管道的耗时,越靠后越能体现Action的执行时间.可根据实际情况灵活配置位置.
/// </summary>
public sealed class ResponseTimeMiddleware
{
    private const string ResponseTime = "EasilyNET-Response-Time";
    private readonly RequestDelegate _next;

    public ResponseTimeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

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

/// <summary>
/// 全局API耗时监控中间件
/// </summary>
// ReSharper disable once UnusedMember.Global
// ReSharper disable once UnusedType.Global
public static class ResponseTimeMiddlewareExtensions
{
    // ReSharper disable once UnusedMember.Global
    public static IApplicationBuilder UseEasilyNETResponseTime(this IApplicationBuilder builder) => builder.UseMiddleware<ResponseTimeMiddleware>();
}