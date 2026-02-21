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
/// <param name="configuration">
///     <para xml:lang="en">
///     <see cref="IConfiguration" /> extracted from the service collection without building a ServiceProvider
///     </para>
///     <para xml:lang="zh">
///     从服务集合中提取的 <see cref="IConfiguration" />，无需构建 ServiceProvider
///     </para>
/// </param>
public sealed class ConfigureServicesContext(IServiceCollection services, IConfiguration configuration)
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
    ///     Gets the configuration directly without building a temporary ServiceProvider.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     直接获取配置，无需构建临时 ServiceProvider。
    ///     </para>
    /// </summary>
    public IConfiguration Configuration { get; } = configuration;

    /// <summary>
    ///     <para xml:lang="en">
    ///     Gets a temporary ServiceProvider for resolving services during configuration.
    ///     WARNING: Each access may build a new temporary ServiceProvider. Prefer using
    ///     <see cref="Configuration" /> for configuration access. Only use this for services
    ///     like IWebHostEnvironment that are not available otherwise.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     获取用于配置期间解析服务的临时 ServiceProvider。
    ///     警告：每次访问可能构建新的临时 ServiceProvider。优先使用 <see cref="Configuration" /> 访问配置。
    ///     仅在需要 IWebHostEnvironment 等无法通过其他方式获取的服务时使用。
    ///     </para>
    /// </summary>
    [Obsolete("Prefer using Configuration property directly. This builds a temporary ServiceProvider which may cause singleton duplication. Will be removed in a future version.")]
    public IServiceProvider ServiceProvider => Services.BuildServiceProvider();

    /// <summary>
    ///     <para xml:lang="en">
    ///     Extracts <see cref="IConfiguration" /> from the service collection without building a ServiceProvider.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     从服务集合中提取 <see cref="IConfiguration" />，无需构建 ServiceProvider。
    ///     </para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    internal static IConfiguration ExtractConfiguration(IServiceCollection services)
    {
        // Try to find IConfiguration already registered as a singleton instance
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is IConfiguration config)
        {
            return config;
        }
        // Fallback: build a minimal temporary provider (only happens if IConfiguration is registered via factory)
        var tempProvider = services.BuildServiceProvider();
        return tempProvider.GetRequiredService<IConfiguration>();
    }
}