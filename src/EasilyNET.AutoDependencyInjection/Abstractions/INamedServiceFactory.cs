namespace EasilyNET.AutoDependencyInjection.Abstractions;

/// <summary>
/// Factory to create named/keyed service instances on demand.
/// </summary>
public interface INamedServiceFactory<out T>
{
    /// <summary>
    /// Create a named/keyed service instance.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    T Create(object key, params Parameter[] parameters);
}