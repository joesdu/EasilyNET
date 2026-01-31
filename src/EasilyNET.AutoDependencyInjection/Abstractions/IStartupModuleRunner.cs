namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <inheritdoc />
internal interface IStartupModuleRunner : IModuleApplication
{
    /// <summary>
    ///     <para xml:lang="en">Initialize all modules synchronously</para>
    ///     <para xml:lang="zh">同步初始化所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The built service provider</para>
    ///     <para xml:lang="zh">已构建的服务提供者</para>
    /// </param>
    void Initialize(IServiceProvider serviceProvider);

    /// <summary>
    ///     <para xml:lang="en">Initialize all modules asynchronously</para>
    ///     <para xml:lang="zh">异步初始化所有模块</para>
    /// </summary>
    /// <param name="serviceProvider">
    ///     <para xml:lang="en">The built service provider</para>
    ///     <para xml:lang="zh">已构建的服务提供者</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task representing the async operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
    Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default);
}