using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">App module interface</para>
///     <para xml:lang="zh">App模块接口</para>
/// </summary>
public interface IAppModule : IApplicationInitialization
{
    /// <summary>
    ///     <para xml:lang="en">
    ///     Configure services synchronously. This method is called during service registration phase,
    ///     before the ServiceProvider is built. Use this for DI registrations only.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     同步配置服务。此方法在服务注册阶段调用，在 ServiceProvider 构建之前。
    ///     仅用于 DI 注册。
    ///     </para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Configuration services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    void ConfigureServices(ConfigureServicesContext context);

    /// <summary>
    ///     <para xml:lang="en">
    ///     Configure services asynchronously. Called after synchronous ConfigureServices.
    ///     Use this for async initialization that doesn't require ServiceProvider.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     异步配置服务。在同步 ConfigureServices 之后调用。
    ///     用于不需要 ServiceProvider 的异步初始化。
    ///     </para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Configuration services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">Cancellation token</para>
    ///     <para xml:lang="zh">取消令牌</para>
    /// </param>
    Task ConfigureServicesAsync(ConfigureServicesContext context, CancellationToken cancellationToken = default);

    /// <summary>
    ///     <para xml:lang="en">Service dependency collection</para>
    ///     <para xml:lang="zh">服务依赖集合</para>
    /// </summary>
    /// <param name="moduleType">
    ///     <para xml:lang="en">Module type</para>
    ///     <para xml:lang="zh">模块类型</para>
    /// </param>
    IEnumerable<Type> GetDependedTypes(Type? moduleType = null);

    /// <summary>
    ///     <para xml:lang="en">Get whether to enable, obtained from the configuration</para>
    ///     <para xml:lang="zh">获取是否启用,从配置中获取</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Configuration services context</para>
    ///     <para xml:lang="zh">配置服务上下文</para>
    /// </param>
    bool GetEnable(ConfigureServicesContext context);
}