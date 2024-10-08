using System.Collections.Concurrent;

namespace EasilyNET.Core.Threading;

/// <summary>
/// 同步信号量。
/// </summary>
internal sealed class SyncSemaphore
{
    private readonly ConcurrentQueue<ManualResetEventSlim> _waiters = [];
    private int _isTaken;

    /// <summary>
    /// 获取是否被占用
    /// </summary>
    /// <returns></returns>
    public int GetTaken() => _isTaken;

    /// <summary>
    /// 获取等待的任务数量
    /// </summary>
    /// <returns></returns>
    public int GetQueueCount() => _waiters.Count;

    /// <summary>
    /// 同步等待
    /// </summary>
    public void Wait()
    {
        // 如果 _isTaken 的值是 0，则将其设置为 1，并返回。
        if (Interlocked.CompareExchange(ref _isTaken, 1, 0) == 0)
        {
            return;
        }
        // 如果 _isTaken 的值不是 0，创建一个新的 ManualResetEventSlim，并将其设置为未终止状态。
        var mre = new ManualResetEventSlim(false);
        // 将 ManualResetEventSlim 实例添加到等待队列中。
        _waiters.Enqueue(mre);
        // 等待 ManualResetEventSlim 被终止。
        mre.Wait();
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Release()
    {
        if (_waiters.TryDequeue(out var toRelease))
        {
            toRelease.Set();
        }
        else
        {
            Interlocked.Exchange(ref _isTaken, 0);
        }
    }
}