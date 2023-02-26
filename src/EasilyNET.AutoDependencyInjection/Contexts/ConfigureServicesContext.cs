using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.Contexts;

/// <summary>
/// 自定义配置服务上下文
/// </summary>
public sealed class ConfigureServicesContext
{
    /// <summary>
    /// 配置服务上下文
    /// </summary>
    /// <param name="services"></param>
    public ConfigureServicesContext(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// IServiceCollection
    /// </summary>
    public IServiceCollection Services { get; }
}