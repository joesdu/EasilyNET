using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">Application initialization interface</para>
///     <para xml:lang="zh">应用初始化接口</para>
/// </summary>
public interface IApplicationInitialization
{
    /// <summary>
    ///     <para xml:lang="en">Synchronous application initialization, called before the async version</para>
    ///     <para xml:lang="zh">同步应用初始化，在异步版本之前调用</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    void ApplicationInitializationSync(ApplicationContext context);

    /// <summary>
    ///     <para xml:lang="en">Asynchronous application initialization</para>
    ///     <para xml:lang="zh">异步应用初始化</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    Task ApplicationInitialization(ApplicationContext context);

    /// <summary>
    ///     <para xml:lang="en">Application shutdown, called in reverse module order when the application stops</para>
    ///     <para xml:lang="zh">应用关闭，在应用停止时按模块逆序调用</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    Task ApplicationShutdown(ApplicationContext context);
}