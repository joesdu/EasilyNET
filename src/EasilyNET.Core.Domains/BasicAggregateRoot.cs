namespace EasilyNET.Core.Domains;

/// <summary>
/// 基类聚合根
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class BasicAggregateRoot<TKey> : Entity<TKey>, IAggregateRoot where TKey : IEquatable<TKey> { }