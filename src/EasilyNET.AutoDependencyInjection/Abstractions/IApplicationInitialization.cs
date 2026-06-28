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
    /// <remarks>
    ///     <para xml:lang="en">
    ///     For every module the framework always invokes BOTH initialization methods in order:
    ///     <see cref="ApplicationInitializationSync" /> first, then <see cref="ApplicationInitialization" />
    ///     (which is awaited; on the synchronous initialization path it is blocked on). If a module overrides
    ///     both methods, both will run — do NOT duplicate the same logic in both, or it will execute twice.
    ///     Put synchronous work here and asynchronous work in the async method.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     对每个模块，框架始终按顺序调用两个初始化方法：先 <see cref="ApplicationInitializationSync" />，
    ///     再 <see cref="ApplicationInitialization" />（异步方法会被 await；在同步初始化路径上则被阻塞等待）。
    ///     若一个模块同时重写两者，二者都会执行——切勿在两个方法里写相同逻辑，否则会执行两次。
    ///     同步工作放在此方法，异步工作放在异步方法。
    ///     </para>
    /// </remarks>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    void ApplicationInitializationSync(ApplicationContext context);

    /// <summary>
    ///     <para xml:lang="en">Asynchronous application initialization</para>
    ///     <para xml:lang="zh">异步应用初始化</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">
    ///     Always runs after <see cref="ApplicationInitializationSync" /> for the same module. On the synchronous
    ///     initialization entry point this task is blocked on, so implementations must not depend on a
    ///     <see cref="System.Threading.SynchronizationContext" /> and should use <c>ConfigureAwait(false)</c>.
    ///     See the remarks on <see cref="ApplicationInitializationSync" /> regarding both methods running.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     对同一模块，总是在 <see cref="ApplicationInitializationSync" /> 之后执行。在同步初始化入口上此任务会被阻塞等待，
    ///     因此实现不得依赖 <see cref="System.Threading.SynchronizationContext" />，并应使用 <c>ConfigureAwait(false)</c>。
    ///     关于两个方法都会执行的说明，参见 <see cref="ApplicationInitializationSync" /> 的备注。
    ///     </para>
    /// </remarks>
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