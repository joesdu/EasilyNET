namespace EasilyNET.Core.Domains;

/// <summary>
/// 工作单元
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// 是否激活事务
    /// </summary>
    bool HasActiveTransaction { get; }

    /// <summary>
    /// 异步保存更改
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步开启事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 异步提交并清除当前事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 回滚事务
    /// </summary>
    /// <param name="cancellationToken"></param>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存所有实体的变更，并协调和分发所有的领域事件
    /// </summary>
    /// <param name="cancellationToken">用于取消任务的令牌</param>
    /// <returns></returns>
    Task<bool> SaveEntitiesAsync(CancellationToken cancellationToken = default);
}