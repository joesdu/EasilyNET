using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
/// <param name="environment">
///     <para xml:lang="en">
///     <see cref="IHostEnvironment" /> extracted from the service collection without building a ServiceProvider (may be null for non-hosted scenarios)
///     </para>
///     <para xml:lang="zh">
///     从服务集合中提取的 <see cref="IHostEnvironment" />，无需构建 ServiceProvider（非托管场景下可能为 null）
///     </para>
/// </param>
public sealed class ConfigureServicesContext(IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment)
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
    ///     Gets the host environment directly without building a temporary ServiceProvider.
    ///     May be null in non-hosted scenarios (e.g. unit tests without a host).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     直接获取宿主环境，无需构建临时 ServiceProvider。
    ///     在非托管场景下（如无宿主的单元测试）可能为 null。
    ///     </para>
    /// </summary>
    public IHostEnvironment? Environment { get; } = environment;

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
        using var tempProvider = services.BuildServiceProvider();
        return tempProvider.GetRequiredService<IConfiguration>();
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Extracts <see cref="IHostEnvironment" /> from the service collection without building a ServiceProvider.
    ///     Returns null if not registered (e.g. non-hosted scenarios).
    ///     </para>
    ///     <para xml:lang="zh">
    ///     从服务集合中提取 <see cref="IHostEnvironment" />，无需构建 ServiceProvider。
    ///     如果未注册（如非托管场景），则返回 null。
    ///     </para>
    /// </summary>
    /// <param name="services">
    ///     <para xml:lang="en">The service collection</para>
    ///     <para xml:lang="zh">服务集合</para>
    /// </param>
    internal static IHostEnvironment? ExtractEnvironment(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IHostEnvironment));
        return descriptor?.ImplementationInstance as IHostEnvironment;
    }
}