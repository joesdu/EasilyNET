namespace EasilyNET.MongoDistributedLock.Attributes;

/// <summary>
/// IDistributedLock
/// </summary>
public interface IDistributedLock
{
    /// <summary>
    /// 获取锁
    /// </summary>
    /// <param name="lifetime"></param>
    /// <param name="timeout"></param>
    /// <returns></returns>
    Task<IAcquire> AcquireAsync(TimeSpan lifetime, TimeSpan timeout);

    /// <summary>
    /// 释放锁
    /// </summary>
    /// <param name="acquire"></param>
    /// <returns></returns>
    Task ReleaseAsync(IAcquire acquire);
}