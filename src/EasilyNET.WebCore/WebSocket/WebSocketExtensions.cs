using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Extension methods for WebSocket.</para>
///     <para xml:lang="zh">WebSocket 扩展方法。</para>
/// </summary>
public static class WebSocketExtensions
{
    /// <summary>
    ///     <para xml:lang="en">Maps a WebSocket handler to a specific path.</para>
    ///     <para xml:lang="zh">将 WebSocket 处理程序映射到特定路径。</para>
    ///     <para xml:lang="en">Note: The handler type <typeparamref name="THandler" /> must be registered in the dependency injection container.</para>
    ///     <para xml:lang="zh">注意：处理程序类型 <typeparamref name="THandler" /> 必须在依赖注入容器中注册。</para>
    /// </summary>
    /// <typeparam name="THandler">
    ///     <para xml:lang="en">The type of the handler.</para>
    ///     <para xml:lang="zh">处理程序的类型。</para>
    /// </typeparam>
    /// <param name="app">
    ///     <para xml:lang="en">The application builder.</para>
    ///     <para xml:lang="zh">应用程序构建器。</para>
    /// </param>
    /// <param name="path">
    ///     <para xml:lang="en">The path to map.</para>
    ///     <para xml:lang="zh">要映射的路径。</para>
    /// </param>
    /// <param name="options">
    ///     <para xml:lang="en">The WebSocket session options.</para>
    ///     <para xml:lang="zh">WebSocket 会话选项。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">The application builder.</para>
    ///     <para xml:lang="zh">应用程序构建器。</para>
    /// </returns>
    public static IApplicationBuilder MapWebSocketHandler<THandler>(this IApplicationBuilder app, PathString path, WebSocketSessionOptions? options = null) where THandler : WebSocketHandler
    {
        if (app.ApplicationServices.GetService(typeof(THandler)) is null)
        {
            throw new InvalidOperationException($"WebSocket handler type '{typeof(THandler).FullName}' is not registered in the dependency injection container. Please register it in ConfigureServices.");
        }
        return app.Map(path, branch => branch.UseMiddleware<WebSocketMiddleware<THandler>>(options ?? new()));
    }
}