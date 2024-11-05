using System.Collections.Concurrent;

namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步信号。
/// </summary>
internal sealed class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(true);
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
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
    /// 异步等待
    /// </summary>
    /// <returns></returns>
    public Task WaitAsync()
    {
        // 如果 _isTaken 的值是 0，则将其设置为 1，并返回一个已完成的任务。
        if (Interlocked.CompareExchange(ref _isTaken, 1, 0) == 0)
        {
            return _completed;
        }
        // 如果 _isTaken 的值不是 0，创建一个新的 TaskCompletionSource<bool>，并将其设置为异步运行。
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        // 将 TaskCompletionSource<bool> 实例添加到等待队列中。
        _waiters.Enqueue(tcs);
        // 返回 TaskCompletionSource<bool> 的任务。
        return tcs.Task;
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Release()
    {
        if (_waiters.TryDequeue(out var toRelease))
        {
            toRelease.SetResult(true);
        }
        else
        {
            Interlocked.Exchange(ref _isTaken, 0);
        }
    }
}