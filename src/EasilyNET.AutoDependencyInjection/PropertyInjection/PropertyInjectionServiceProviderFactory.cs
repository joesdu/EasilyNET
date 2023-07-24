using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入服务提供者工厂
/// </summary>
internal sealed class PropertyInjectionServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    /// <summary>
    /// 从容器生成器创建 IServiceProvider。
    /// </summary>
    /// <param name="containerBuilder">容器生成器。</param>
    /// <returns>返回IServiceProvider</returns>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) => new PropertyInjectionServiceProvider(containerBuilder);

    /// <summary>
    /// 从 IServiceCollection 创建容器生成器。
    /// </summary>
    /// <param name="services">服务的集合。</param>
    /// <returns>IServiceCollection 可用于创建 IServiceProvider 的容器生成器。</returns>
    public IServiceCollection CreateBuilder(IServiceCollection? services) => services ?? new ServiceCollection();
}