namespace EasilyNET.Core.DeepCopy;

/// <summary>
/// ReferenceEqualityComparer
/// </summary>
internal class ReferenceEqualityComparer : EqualityComparer<object?>
{
    /// <inheritdoc />
    public override bool Equals(object? x, object? y) => ReferenceEquals(x, y);

    /// <inheritdoc />
    public override int GetHashCode(object? obj) => obj is null ? 0 : obj.GetHashCode();
}