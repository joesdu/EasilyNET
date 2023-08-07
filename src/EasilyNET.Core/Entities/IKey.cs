namespace EasilyNET.Core.Entities;

/// <summary>
/// 标识
/// </summary>
public interface IKey<out TKey>
{
    /// <summary>
    /// 标识
    /// </summary>
    TKey Id { get; }
}