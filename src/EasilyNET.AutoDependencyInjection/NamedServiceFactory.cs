using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection;

internal sealed class NamedServiceFactory<T>(IServiceProvider provider) : INamedServiceFactory<T>
{
    public T Create(object key, params Parameter[] parameters)
    {
        using var resolver = new Resolver(provider);
        return resolver.ResolveKeyed<T>(key, parameters);
    }
}
