namespace EasilyNET.Core.IUnitOfWork;

/// <summary>
/// IUnitOfWork
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// 异步提交
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> CommitAsync(CancellationToken cancellationToken = default);
}