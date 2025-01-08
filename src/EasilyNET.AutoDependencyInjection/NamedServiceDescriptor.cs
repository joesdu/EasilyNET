using Microsoft.Extensions.DependencyInjection;

namespace EasilyNET.AutoDependencyInjection;

internal sealed class NamedServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime)
{
    public Type ServiceType { get; } = serviceType;

    public Type ImplementationType { get; } = implementationType;

    public ServiceLifetime Lifetime { get; } = lifetime;
}