namespace EasilyNET.EntityFrameworkCore;

/// <summary>
/// 工作单元
/// </summary>
public interface IUnitOfWork: IDisposable
{
    
    /// <summary>
    /// 异步保存更改
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 是否激活事务
    /// </summary>
    bool HasActiveTransaction { get; }



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
}