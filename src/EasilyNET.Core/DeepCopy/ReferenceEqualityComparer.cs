namespace EasilyNET.Core.DeepCopy;

/// <inheritdoc />
internal class ReferenceEqualityComparer : EqualityComparer<object?>
{
    /// <inheritdoc />
    public override bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public override int GetHashCode(object? obj) => obj is null ? 0 : obj.GetHashCode();
}