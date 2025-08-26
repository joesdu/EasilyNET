// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
/// Parameter abstraction for dynamic constructor selection/overrides inspired by Autofac.
/// </summary>
public abstract class Parameter
{
    internal abstract bool CanSupplyValue(Type parameterType, string? parameterName);
    internal abstract object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName);
}

/// <summary>
/// NamedParameter matches a parameter by name.
/// </summary>
/// <param name="name"></param>
/// <param name="value"></param>
public sealed class NamedParameter(string name, object? value) : Parameter
{
    /// <summary>
    /// Name of the parameter to match.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Value to supply for the parameter.
    /// </summary>
    public object? Value { get; } = value;

    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => string.Equals(parameterName, Name, StringComparison.Ordinal);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => Value;
}

/// <summary>
/// TypedParameter matches a parameter by type.
/// </summary>
/// <param name="type"></param>
/// <param name="value"></param>
public sealed class TypedParameter(Type type, object? value) : Parameter
{
    /// <summary>
    /// Type of the parameter to match.
    /// </summary>
    public Type Type { get; } = type;

    /// <summary>
    /// Value to supply for the parameter.
    /// </summary>
    public object? Value { get; } = value;

    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => Type == parameterType || parameterType.IsAssignableFrom(Type);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => Value;
}

/// <summary>
/// ResolvedParameter matches a parameter by predicate, and uses a value accessor to supply the value.
/// </summary>
/// <param name="predicate"></param>
/// <param name="valueAccessor"></param>
public sealed class ResolvedParameter(Func<Type, string?, bool> predicate, Func<IServiceProvider, Type, string?, object?> valueAccessor) : Parameter
{
    internal override bool CanSupplyValue(Type parameterType, string? parameterName) => predicate(parameterType, parameterName);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName) => valueAccessor(provider, parameterType, parameterName);
}