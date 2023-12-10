namespace EasilyNET.Core.Domains;

/// <summary>
/// 聚合根实现并版本号
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class AggregateRootWithRowVersion<TKey> : AggregateRoot<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 并发版本号
    /// </summary>
    public byte[] Version { get; set; } = default!;
}