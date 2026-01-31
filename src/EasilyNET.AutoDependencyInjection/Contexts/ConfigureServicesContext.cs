using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <summary>
///     <para xml:lang="en">Custom configuration services context for service registration phase</para>
///     <para xml:lang="zh">用于服务注册阶段的自定义配置服务上下文</para>
/// </summary>
/// <param name="services">
///     <para xml:lang="en">
///         <see cref="IServiceCollection" />
///     </para>
///     <para xml:lang="zh">
///         <see cref="IServiceCollection" />
///     </para>
/// </param>
public sealed class ConfigureServicesContext(IServiceCollection services) : IDisposable
{
    private IServiceProvider? _serviceProvider;

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
    ///     Gets a temporary ServiceProvider for resolving services during configuration.
    ///     WARNING: This provider only contains services registered BEFORE this point.
    ///     Use sparingly and prefer IConfiguration for configuration access.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取用于配置期间解析服务的临时 ServiceProvider。
    ///     警告：此提供者仅包含在此之前注册的服务。
    ///     请谨慎使用，优先使用 IConfiguration 访问配置。
    ///     </para>
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider ??= Services.BuildServiceProvider();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets the configuration. This is a convenience method equivalent to
    ///     ServiceProvider.GetRequiredService&lt;IConfiguration&gt;().
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取配置。这是一个便捷方法，等同于
    ///     ServiceProvider.GetRequiredService&lt;IConfiguration&gt;()。
    ///     </para>
    /// </summary>
    public IConfiguration Configuration => ServiceProvider.GetRequiredService<IConfiguration>();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Disposes the temporary ServiceProvider if it was created.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     释放临时 ServiceProvider（如果已创建）。
    ///     </para>
    /// </summary>
    public void Dispose()
    {
        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}