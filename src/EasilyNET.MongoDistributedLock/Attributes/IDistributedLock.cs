namespace EasilyNET.MongoDistributedLock.Core.Attributes;

/// <summary>
/// IDistributedLock
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="lifetime"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout);

    /// <summary>
    /// 释放
    /// </summary>
    /// <param name="acquire"></param>
    /// <returns></returns>
    Task ReleaseAsync(IAcquire acquire);

    /// <summary>
    /// 获取
    /// </summary>
    /// <param name="lifetime"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    IAcquire Acquire(TimeSpan lifetime, TimeSpan timeout);

    /// <summary>
    /// 释放
    /// </summary>
    /// <param name="acquire"></param>
    /// <returns></returns>
    void Release(IAcquire acquire);
}