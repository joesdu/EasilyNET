using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <summary>
///     <para xml:lang="en">Custom configuration services context</para>
///     <para xml:lang="zh">自定义配置服务上下文</para>
/// </summary>
/// <param name="services">
///     <para xml:lang="en">
///         <see cref="IServiceCollection" />
///     </para>
///     <para xml:lang="zh">
///         <see cref="IServiceCollection" />
///     </para>
/// </param>
/// <param name="provider">
///     <para xml:lang="en">
///         <see cref="IServiceProvider" />
///     </para>
///     <para xml:lang="zh">
///         <see cref="IServiceProvider" />
///     </para>
/// </param>
public sealed class ConfigureServicesContext(IServiceCollection services, IServiceProvider provider)
{
    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceCollection" />
    ///     </para>
    /// </summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    ///     <para xml:lang="en">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    ///     <para xml:lang="zh">
    ///         <see cref="IServiceProvider" />
    ///     </para>
    /// </summary>
    public IServiceProvider ServiceProvider { get; } = provider;
}