using System.Collections.Concurrent;

namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步信号。
/// </summary>
internal sealed class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(true);
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
    private int _isTaken = 0;  
    
    /// <summary>
    /// 获取是否被占用
    /// </summary>
    /// <returns></returns>
    public int GetTaken() => _isTaken;
    
    public int GetQueueCount() => _waiters.Count;

    /// <summary>
    /// 异步等待
    /// </summary>
    /// <returns></returns>
    public Task WaitAsync()
    {
        if (Interlocked.CompareExchange(ref _isTaken, 1, 0) == 0)
        {
            return _completed;
        }
        else
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Enqueue(tcs);
            return tcs.Task;
        }
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