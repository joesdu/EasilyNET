using EasilyNET.AutoDependencyInjection.Contexts;

namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
///     <para xml:lang="en">Application initialization interface</para>
///     <para xml:lang="zh">应用初始化接口</para>
/// </summary>
public interface IApplicationInitialization
{
    /// <summary>
    ///     <para xml:lang="en">Application initialization</para>
    ///     <para xml:lang="zh">应用初始化</para>
    /// </summary>
    /// <param name="context">
    ///     <para xml:lang="en">Application context</para>
    ///     <para xml:lang="zh">应用上下文</para>
    /// </param>
    Task ApplicationInitialization(ApplicationContext context);
}