using EasilyNET.WebCore.Middleware;
using EasilyNET.WebCore.WebSocket;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    /// <param name="services">
    ///     <para xml:lang="en">The service collection.</para>
    ///     <para xml:lang="zh">服务集合。</para>
    /// </param>
    extension(IServiceCollection services)
    {
        /// <summary>
        ///     <para xml:lang="en">Adds the WebSocket session manager to the service collection as a singleton.</para>
        ///     <para xml:lang="zh">将 WebSocket 会话管理器作为单例添加到服务集合中。</para>
        /// </summary>
        /// <returns>
        ///     <para xml:lang="en">The service collection for chaining.</para>
        ///     <para xml:lang="zh">用于链式调用的服务集合。</para>
        /// </returns>
        /// <remarks>
        ///     <para xml:lang="en">
        ///     Call this method to enable session tracking and broadcast capabilities.
        ///     The session manager can be injected into handlers or other services to access active sessions.
        ///     </para>
        ///     <para xml:lang="zh">
        ///     调用此方法以启用会话跟踪和广播功能。
        ///     会话管理器可以注入到处理程序或其他服务中以访问活动会话。
        ///     </para>
        /// </remarks>
        public IServiceCollection AddWebSocketSessionManager()
        {
            services.TryAddSingleton<WebSocketSessionManager>();
            services.TryAddSingleton<IWebSocketSessionManager>(sp => sp.GetRequiredService<WebSocketSessionManager>());
            return services;
        }
    }

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