namespace EasilyNET.Core.Domains;

/// <summary>
/// 聚合根
/// </summary>
/// <typeparam name="TKey">主键类型</typeparam>
public abstract class AggregateRoot<TKey> : AggregateRootBase<TKey> where TKey : IEquatable<TKey> { }