namespace EasilyNET.Core.Domains;

/// <summary>
/// 用来表示聚合根
/// </summary>
public interface IAggregateRoot;

/// <summary>
/// 用来表示聚合根
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IAggregateRoot<TKey> : IAggregateRoot, IEntity<TKey> where TKey : IEquatable<TKey> { }