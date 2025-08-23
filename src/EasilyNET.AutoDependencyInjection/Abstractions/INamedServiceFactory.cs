namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// Factory to create named/keyed service instances on demand.
/// </summary>
public interface INamedServiceFactory<out T>
{
    T Create(object key, params Parameter[] parameters);
}
