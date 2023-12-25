namespace EasilyNET.Core.Domains;

/// <summary>
/// 创建事件
/// </summary>
public interface IGenerateDomainEvents
{
    /// <summary>
    /// 得到领域事件集合
    /// </summary>
    IReadOnlyCollection<IDomainEvent>? GetDomainEvents();

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    void AddDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// 移除领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    void RemoveDomainEvent(IDomainEvent domainEvent);

    /// <summary>
    /// 清空领域事件
    /// </summary>
    void ClearDomainEvent();
}