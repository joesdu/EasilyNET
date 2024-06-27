using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <summary>
/// 自定义配置服务上下文
/// </summary>
/// <param name="services"></param>
/// <param name="provider"></param>
public sealed class ConfigureServicesContext(IServiceCollection services, IServiceProvider? provider)
{
    /// <summary>
    ///     <see cref="IServiceCollection" />
    /// </summary>
    public IServiceCollection Services { get; } = services;

    /// <summary>
    ///     <see cref="IServiceProvider" />
    /// </summary>
    public IServiceProvider? ServiceProvider { get; } = provider;
}