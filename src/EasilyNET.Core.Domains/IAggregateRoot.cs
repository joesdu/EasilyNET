namespace EasilyNET.Core.Domains;

/// <summary>
/// 用来表示聚合根
/// </summary>
public interface IAggregateRoot : IHasRowVersion;

/// <summary>
/// 聚合根实现
/// </summary>
/// <typeparam name="TKey"></typeparam>
public class AggregateRoot<TKey> : Entity<TKey> where TKey : IEquatable<TKey>, IAggregateRoot
{
    /// <summary>
    /// 并发版本号
    /// </summary>
    public byte[] Version { get; set; } = default!;
}