using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入服务提供者工厂
/// </summary>
public class PropertyInjectionServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    /// <summary>
    /// </summary>
    /// <param name="containerBuilder"></param>
    /// <returns></returns>
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        var serviceProvider = containerBuilder.BuildServiceProvider();
        return new PropertyInjectionServiceProvider(serviceProvider);
    }

    IServiceCollection IServiceProviderFactory<IServiceCollection>.CreateBuilder(IServiceCollection? services) => services ?? new ServiceCollection();
}