namespace EasilyNET.Core.Entities;

/// <summary>
/// 实体接口
/// </summary>
public interface IEntity
{

    /// <summary>
    /// 领域事件集合
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get;  }

    /// <summary>
    /// 清空领域事件
    /// </summary>
    void ClearDomainEvent();
}


/// <summary>
/// 实体接口
/// </summary>
/// <typeparam name="TKey"></typeparam>
public interface IEntity<out TKey>:IEntity,IKey<TKey>
{
    
}