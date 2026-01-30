namespace EasilyNET.WebCore.WebSocket;

/// <summary>
///     <para xml:lang="en">Internal interface for session registration operations used by middleware.</para>
///     <para xml:lang="zh">供中间件使用的会话注册操作内部接口。</para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     This interface is internal and used by the WebSocket middleware to register and unregister sessions.
///     Custom implementations of <see cref="IWebSocketSessionManager" /> should also implement this interface
///     to enable session tracking.
///     </para>
///     <para xml:lang="zh">
///     此接口为内部接口，供 WebSocket 中间件用于注册和注销会话。
///     <see cref="IWebSocketSessionManager" /> 的自定义实现也应实现此接口以启用会话跟踪。
///     </para>
/// </remarks>
internal interface IWebSocketSessionRegistry
{
    /// <summary>
    ///     <para xml:lang="en">Adds a session to the registry.</para>
    ///     <para xml:lang="zh">将会话添加到注册表。</para>
    /// </summary>
    /// <param name="session">
    ///     <para xml:lang="en">The session to add.</para>
    ///     <para xml:lang="zh">要添加的会话。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the session was added; false if a session with the same ID already exists.</para>
    ///     <para xml:lang="zh">如果会话已添加则返回 true；如果已存在相同 ID 的会话则返回 false。</para>
    /// </returns>
    bool AddSession(IWebSocketSession session);

    /// <summary>
    ///     <para xml:lang="en">Removes a session from the registry.</para>
    ///     <para xml:lang="zh">从注册表中移除会话。</para>
    /// </summary>
    /// <param name="id">
    ///     <para xml:lang="en">The ID of the session to remove.</para>
    ///     <para xml:lang="zh">要移除的会话的 ID。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">True if the session was removed; false if no session with the given ID was found.</para>
    ///     <para xml:lang="zh">如果会话已移除则返回 true；如果未找到给定 ID 的会话则返回 false。</para>
    /// </returns>
    bool RemoveSession(string id);
}