namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    ///     <para xml:lang="en">Initialize all modules synchronously</para>
    ///     <para xml:lang="zh">同步初始化所有模块</para>
    /// </summary>
    // TODO?: [Obsolete("Use InitializeAsync method instead.")]
    void Initialize();

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules asynchronously</para>
    ///     <para xml:lang="zh">异步初始化所有模块</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task representing the async operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    Task InitializeAsync(CancellationToken cancellationToken = default);
}