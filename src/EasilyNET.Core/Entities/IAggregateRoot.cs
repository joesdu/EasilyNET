namespace EasilyNET.Core.Entities;

/// <summary>
/// 聚合根
/// </summary>
public interface IAggregateRoot : IEntity { }

/// <summary>
/// 聚合根
/// </summary>
/// <typeparam name="TKey">标识类型</typeparam>
public interface IAggregateRoot<out TKey> : IEntity<TKey>, IAggregateRoot { }

/// <summary>
/// 聚合根
/// </summary>
/// <typeparam name="TKey">标识类型</typeparam>
public abstract class AggregateRoot<TKey> : EntityBase<TKey>, IAggregateRoot<TKey>
    where TKey : IEquatable<TKey> { }

/// <summary>
/// 全部聚合根
/// </summary>
/// <typeparam name="TKey">主键</typeparam>
/// <typeparam name="TUserKey">用户ID</typeparam>
public abstract class FullAggregateRoot<TKey, TUserKey> : AggregateRoot<TKey>, IHasSoftDelete, IHasCreateTime,
    IHasUpdatedTime, IHasCreateUserId<TUserKey>
    where TKey : IEquatable<TKey>
    where TUserKey : IEquatable<TUserKey>
{
    /// <inheritdoc />
    public virtual DateTime? CreateTime { get; protected set; }

    /// <inheritdoc />
    public virtual TUserKey? CreateUserId { get; protected set; }

    /// <inheritdoc />
    public virtual bool IsDelete { get; protected set; }

    /// <inheritdoc />
    public virtual DateTime? UpdatedTime { get; protected set; }
}