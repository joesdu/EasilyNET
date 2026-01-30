using EasilyNET.WebCore.WebSocket;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable UnusedMethodReturnValue.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;

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
}