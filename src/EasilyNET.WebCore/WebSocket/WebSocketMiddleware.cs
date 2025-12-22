using Microsoft.AspNetCore.Http;

namespace EasilyNET.WebCore.WebSocket;

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
/// <param name="handler">
///     <para xml:lang="en">The WebSocket handler.</para>
///     <para xml:lang="zh">WebSocket 处理程序。</para>
/// </param>
public class WebSocketMiddleware<THandler>(RequestDelegate next, THandler handler) where THandler : WebSocketHandler
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
            var session = new WebSocketSession(context.TraceIdentifier, webSocket, handler);
            await session.ProcessAsync(context.RequestAborted);
        }
        else
        {
            await next(context);
        }
    }
}