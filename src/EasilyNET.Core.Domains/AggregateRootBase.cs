namespace EasilyNET.Core.Domains;

/// <summary>
/// 聚合根
/// </summary>
public abstract class AggregateRootBase : Entity, IAggregateRoot, IGenerateDomainEvents
{
    /// <summary>
    /// 领域事件不映射
    /// </summary>
    [NotMapped]
    private List<IDomainEvent>? _domainEvents;

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= [];
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 移除领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents?.Remove(domainEvent);

    /// <summary>
    /// 清空领域事件
    /// </summary>
    public void ClearDomainEvent() => _domainEvents?.Clear();

    /// <summary>
    /// 得到领域事件集合
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<IDomainEvent>? GetDomainEvents() => _domainEvents?.AsReadOnly();
}

/// <summary>
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class AggregateRootBase<TKey> : Entity<TKey>, IAggregateRoot<TKey>
    , IGenerateDomainEvents
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 领域事件不映射
    /// </summary>
    [NotMapped]
    private List<IDomainEvent>? _domainEvents;

    /// <summary>
    /// 得到领域事件集合
    /// </summary>
    /// <returns></returns>
    public IReadOnlyCollection<IDomainEvent>? GetDomainEvents() => _domainEvents?.AsReadOnly();

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= [];
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 移除领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents?.Remove(domainEvent);

    /// <summary>
    /// 清空领域事件
    /// </summary>
    public void ClearDomainEvent() => _domainEvents?.Clear();
}