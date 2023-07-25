using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection.PropertyInjection;

/// <summary>
/// 属性注入服务提供者工厂
/// </summary>
internal sealed class PropertyInjectionServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    /// <inheritdoc />
    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder) => new PropertyInjectionServiceProvider(containerBuilder);

    /// <inheritdoc />
    public IServiceCollection CreateBuilder(IServiceCollection? services) => services ?? new ServiceCollection();
}