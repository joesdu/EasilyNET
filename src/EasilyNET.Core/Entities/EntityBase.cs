using EasilyNET.Core.Misc;

namespace EasilyNET.Core.Entities;

/// <summary>
/// 实体基类
/// </summary>
/// <typeparam name="TKey"></typeparam>
public abstract class EntityBase<TKey>:IEntity<TKey>
    where TKey : IEquatable<TKey>
{
    /// <summary>
    /// 领域事件
    /// </summary>

    private  List<IDomainEvent>? _domainEvents;
    /// <summary>
    /// 
    /// </summary>
    protected EntityBase()
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="id"></param>
    protected EntityBase(TKey id)
    {
        Id = id;
    }
    
    /// <summary>
    /// 主键
    /// </summary>
    public virtual TKey Id { get; protected set; }
    
    /// <summary>
    /// 判断两个实体是否是同一数据记录的实体
    /// </summary>
    /// <param name="obj">要比较的实体信息</param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (obj == null || !(obj is EntityBase<TKey>))
        {
            return false;
        }


        if (ReferenceEquals(this, obj))
        {
            return true;
        }
        if (this.GetType() != obj.GetType())
        {
            return false;
        }
        if (!(obj is EntityBase<TKey> entity))
        {
            return false;
        }

        if (IsTransient() || entity.IsTransient())
        {
            return false;
        }
        
        return IsKeyEqual(entity.Id, Id);
    }

  

    protected virtual bool IsTransient()
    {
        
        if (typeof(TKey) == typeof(int))
        {
            return Convert.ToInt32(Id) <= 0;
        }
        if (typeof(TKey) == typeof(long))
        {
            return Convert.ToInt64(Id) <= 0;
        }


        //Guid
        if (typeof(TKey) == typeof(Guid))
        {
            
            return Guid.Empty.Equals(Id);
        }
        return false;
    }
    



    /// <summary>
    /// 实体ID是否相等
    /// </summary>
    private  bool IsKeyEqual(TKey id1, TKey id2)
    {
        if (id1 == null && id2 == null)
        {
            return true;
        }
        if (id1 == null || id2 == null)
        {
            return false;
        }
        
        

        return Equals(id1, id2);
    }

    /// <summary>
    /// 用作特定类型的哈希函数。
    /// </summary>
    /// <returns>
    /// 当前 <see cref="T:System.Object"/> 的哈希代码。 <br/> 如果 <c>Id</c> 为 <c>null</c> 则返回0， 如果不为
    /// <c>null</c> 则返回 <c>Id</c> 对应的哈希值
    /// </returns>
    public override int GetHashCode()
    {
        return ReferenceEquals( Id, null ) ? 0 : Id.GetHashCode();
    }

    /// <summary>
    /// 等于
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator ==(EntityBase<TKey> left, EntityBase<TKey> right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// 不等于
    /// </summary>
    /// <param name="left"></param>
    /// <param name="right"></param>
    /// <returns></returns>
    public static bool operator !=(EntityBase<TKey> left, EntityBase<TKey> right)
    {
        return !(left == right);
    }

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    protected  void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents ??= new();
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 移除领域事件
    /// </summary>
    /// <param name="domainEvent">领域事件</param>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
         _domainEvents?.Remove(domainEvent);
    }



    /// <summary>
    /// 领域事件集合
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents  => _domainEvents?.AsReadOnly()!;

    /// <summary>
    /// 清空领域事件
    /// </summary>
    public void ClearDomainEvent()
    {
        _domainEvents?.Clear();
    }
}

/// <summary>
/// 实体基类 long类型
/// </summary>
public abstract class EntityBase : EntityBase<long>
{
    
}