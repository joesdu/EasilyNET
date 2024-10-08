// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
/// 同步锁,用于仅让一个线程访问共享资源。
/// </summary>
public sealed class SyncLock
{
    private readonly SyncSemaphore _semaphore = new();
    private int _ownerThreadId = -1;

    /// <summary>
    /// 获取内部信号量的占用状态
    /// </summary>
    /// <returns></returns>
    public int GetSemaphoreTaken() => _semaphore.GetTaken();

    /// <summary>
    /// 获取内部信号量的队列计数
    /// </summary>
    /// <returns></returns>
    public int GetQueueCount() => _semaphore.GetQueueCount();

    /// <summary>
    /// 锁定,返回一个 <see cref="Release" /> 对象
    /// </summary>
    /// <returns></returns>
    public Release Lock()
    {
        var currentThreadId = Environment.CurrentManagedThreadId;
        if (_ownerThreadId == currentThreadId)
        {
            throw new InvalidOperationException("Reentrant lock detected");
        }
        _semaphore.Wait();
        _ownerThreadId = currentThreadId;
        return new(this);
    }

    /// <summary>
    /// 锁定任务的执行,无返回值
    /// </summary>
    /// <param name="action"></param>
    public void Lock(Action action)
    {
        using var r = Lock();
        action();
    }

    /// <summary>
    /// 锁定任务的执行,可返回执行函数的结果
    /// </summary>
    /// <param name="action"></param>
    public T Lock<T>(Func<T> action)
    {
        using var r = Lock();
        return action();
    }

    /// <remarks>
    /// Release
    /// </remarks>
    /// <param name="syncLock"></param>
    public readonly struct Release(SyncLock syncLock) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            syncLock._ownerThreadId = -1;
            syncLock._semaphore.Release();
        }
    }
}