// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

namespace EasilyNET.AutoDependencyInjection;

/// <summary>
/// Parameter abstraction for dynamic constructor selection/overrides inspired by Autofac.
/// </summary>
public abstract class Parameter
{
    internal abstract bool CanSupplyValue(Type parameterType, string? parameterName, int position);
    internal abstract object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName, int position);
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

    internal override bool CanSupplyValue(Type parameterType, string? parameterName, int position) => string.Equals(parameterName, Name, StringComparison.Ordinal);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName, int position) => Value;
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

    internal override bool CanSupplyValue(Type parameterType, string? parameterName, int position) => Type == parameterType || parameterType.IsAssignableFrom(Type);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName, int position) => Value;
}

/// <summary>
/// PositionalParameter matches a parameter by its position (zero-based index) in the constructor.
/// </summary>
/// <param name="position">Zero-based position of the constructor parameter.</param>
/// <param name="value">Value to supply for the parameter.</param>
public sealed class PositionalParameter(int position, object? value) : Parameter
{
    /// <summary>
    /// Zero-based position of the constructor parameter to match.
    /// </summary>
    public int Position { get; } = position;

    /// <summary>
    /// Value to supply for the parameter.
    /// </summary>
    public object? Value { get; } = value;

    internal override bool CanSupplyValue(Type parameterType, string? parameterName, int pos) => pos == Position;
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName, int pos) => Value;
}

/// <summary>
/// ResolvedParameter matches a parameter by predicate, and uses a value accessor to supply the value.
/// </summary>
/// <param name="predicate"></param>
/// <param name="valueAccessor"></param>
public sealed class ResolvedParameter(Func<Type, string?, bool> predicate, Func<IServiceProvider, Type, string?, object?> valueAccessor) : Parameter
{
    internal override bool CanSupplyValue(Type parameterType, string? parameterName, int position) => predicate(parameterType, parameterName);
    internal override object? GetValue(IServiceProvider provider, Type parameterType, string? parameterName, int position) => valueAccessor(provider, parameterType, parameterName);
}