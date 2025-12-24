using EasilyNET.WebCore.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace EasilyNET.WebCore.Middleware;

/// <summary>
///     <para xml:lang="en">Middleware for handling WebSocket requests.</para>
///     <para xml:lang="zh">处理 WebSocket 请求的中间件。</para>
/// </summary>
/// <typeparam name="THandler">
///     <para xml:lang="en">The type of the handler.</para>
///     <para xml:lang="zh">处理程序的类型。</para>
/// </typeparam>
/// <param name="next">
///     <para xml:lang="en">The next middleware in the pipeline.</para>
///     <para xml:lang="zh">管道中的下一个中间件。</para>
/// </param>
/// <param name="options">
///     <para xml:lang="en">The WebSocket session options.</para>
///     <para xml:lang="zh">WebSocket 会话选项。</para>
/// </param>
/// <param name="handler">
///     <para xml:lang="en">The WebSocket handler.</para>
///     <para xml:lang="zh">WebSocket 处理程序。</para>
/// </param>
/// <param name="logger">
///     <para xml:lang="en">The logger.</para>
///     <para xml:lang="zh">日志记录器。</para>
/// </param>
internal sealed class WebSocketMiddleware<THandler>(RequestDelegate next, WebSocketSessionOptions options, THandler handler, ILogger<WebSocketSession> logger) where THandler : WebSocketHandler
{
    /// <summary>
    ///     <para xml:lang="en">Invokes the middleware.</para>
    ///     <para xml:lang="zh">调用中间件。</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">The HTTP context.</para>
    ///     <para xml:lang="zh">HTTP 上下文。</para>
    /// </param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest)
        {
            using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
            var session = new WebSocketSession(context.TraceIdentifier, webSocket, handler, options, logger);
            await session.ProcessAsync(context.RequestAborted);
        }
        else
        {
            await next(context);
        }
    }
}