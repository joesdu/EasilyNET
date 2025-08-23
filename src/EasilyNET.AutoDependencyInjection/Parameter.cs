namespace EasilyNET.AutoDependencyInjection;

/// <summary>
/// Parameter abstraction for dynamic constructor selection/overrides inspired by Autofac.
/// </summary>
public abstract class Parameter
{
    internal abstract bool CanSupplyValue(Type parameterType, string? parameterName);
    internal abstract object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName);
}

public sealed class NamedParameter(string name, object? value) : Parameter
{
    public string Name { get; } = name;
    public object? Value { get; } = value;

    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => string.Equals(parameterName, Name, StringComparison.Ordinal);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => Value;
}

public sealed class TypedParameter(Type type, object? value) : Parameter
{
    public Type Type { get; } = type;
    public object? Value { get; } = value;

    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => Type == parameterType || parameterType.IsAssignableFrom(Type);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => Value;
}

public sealed class ResolvedParameter(Func<Type, string?, bool> predicate, Func<IServiceProvider, Type, string?, object?> valueAccessor) : Parameter
{
    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => predicate(parameterType, parameterName);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => valueAccessor(provider, parameterType, parameterName);
}
