namespace EasilyNET.Core.Entities;

/// <summary>
/// 标识
/// </summary>
public interface IKey<out TKey>
{
    /// <summary>
    /// ID
    /// </summary>
    TKey Id { get; }
}