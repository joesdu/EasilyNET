using EasilyNET.AutoDependencyInjection.Abstractions;

namespace EasilyNET.AutoDependencyInjection;

internal sealed class ObjectAccessor<T> : IObjectAccessor<T>
{
    public T? Value { get; set; }
}