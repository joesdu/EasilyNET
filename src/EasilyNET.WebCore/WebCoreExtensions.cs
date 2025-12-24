using EasilyNET.WebCore.Middleware;
using EasilyNET.WebCore.WebSocket;
using Microsoft.AspNetCore.Http;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.AspNetCore.Builder;

/// <summary>
///     <para xml:lang="en">Middleware extensions for unified handling of middleware invocation extensions</para>
///     <para xml:lang="zh">中间件扩展，用于统一处理中间件调用扩展</para>
/// </summary>
public static class WebCoreExtensions
{
    /// <param name="builder">
    ///     <para xml:lang="en">The application builder.</para>
    ///     <para xml:lang="zh">应用程序构建器。</para>
    /// </param>
    extension(IApplicationBuilder builder)
    {
        /// <summary>
        ///     <para xml:lang="en">Uses the global API response time monitoring middleware</para>
        ///     <para xml:lang="zh">使用全局 API 耗时监控中间件</para>
        /// </summary>
        public IApplicationBuilder UseResponseTime() => builder.UseMiddleware<ResponseTimeMiddleware>();

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
        public IApplicationBuilder MapWebSocketHandler<THandler>(PathString path, WebSocketSessionOptions? options = null) where THandler : WebSocketHandler
        {
            return builder.ApplicationServices.GetService(typeof(THandler)) is null
                       ? throw new InvalidOperationException($"WebSocket handler type '{typeof(THandler).FullName}' is not registered in the dependency injection container. Please register it in ConfigureServices.")
                       : builder.Map(path, branch => branch.UseMiddleware<WebSocketMiddleware<THandler>>(options ?? new()));
        }
    }
}