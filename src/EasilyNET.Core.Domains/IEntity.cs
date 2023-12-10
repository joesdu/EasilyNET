namespace EasilyNET.Core.Domains;

/// <summary>
/// 实现接口
/// </summary>
/// <typeparam name="TKey">动态键</typeparam>
public interface IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 主键
    /// </summary>
    TKey Id { get; }
}