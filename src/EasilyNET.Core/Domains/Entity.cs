using System.ComponentModel.DataAnnotations.Schema;

namespace EasilyNET.Core.Domains;


//ReSharper disable VirtualMemberNeverOverridden.Global
//ReSharper disable MemberCanBeProtected.Global

/// <summary>
/// 实体共用
/// </summary>
public abstract class Entity
{


    /// <summary>
    /// 领域事件不映射
    /// </summary>
    
    [NotMapped]
    private List<IDomainEvent>? _domainEvents;

    /// <summary>
    /// 领域事件集合
    /// </summary>
    [NotMapped]
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents?.AsReadOnly()!;

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= new();
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

/// <summary>
/// 泛型实体，用来限定。
/// </summary>
/// <typeparam name="TKey">动态类型</typeparam>

public abstract class Entity<TKey> : Entity where TKey : IEquatable<TKey>
{



    // /// <summary>
    // /// 
    // /// </summary>
    // /// <param name="id"></param>
    // protected Entity(TKey id) : this()
    // {
    //     
    //     
    // }

    
    

    /// <summary>
    /// 主键
    /// </summary>
    public  virtual TKey Id { get; protected set; } = default!;

    /// <summary>
    ///  比较是否值和引用都相等
    /// </summary>
    /// <param name="obj">要比较的类型</param>
    /// <returns></returns>
    public override bool Equals(object? obj)
    {
#pragma warning disable IDE0046 // 转换为条件表达式
        if (obj is not Entity<TKey> entity)
        {
            return false;
        }
        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (GetType() != obj.GetType())
        {
            return false;
        }

        if (IsTransient() || entity.IsTransient())
        {
            return false;
        }
        return entity.Id.Equals(Id);

#pragma warning restore IDE0046 // 转换为条件表达式
    }


/// <summary>
/// 表示对象是否为全新创建的，未持久化的
/// </summary>
/// <returns></returns>
    public virtual bool IsTransient() => EqualityComparer<TKey>.Default.Equals(Id, default);

    /// <summary>
    /// 用作特定类型的哈希函数。
    /// </summary>
    /// <returns>
    /// 当前 <see cref="T:System.Object" /> 的哈希代码。 <br /> 如果 <c>Id</c> 为 <c>null</c> 则返回0， 如果不为
    /// <c>null</c> 则返回 <c>Id</c> 对应的哈希值
    /// </returns>
    public override int GetHashCode()
    {

        if (!IsTransient())
        {
            return Id.GetHashCode() ^ 31;

        }
        return Id.GetHashCode();
    }
    

    /// <summary>
    /// 等于
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(Entity<TKey> left, Entity<TKey> right) => Equals(left, right);

    /// <summary>
    /// 不等于
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(Entity<TKey> left, Entity<TKey> right) => !(left == right);

}


