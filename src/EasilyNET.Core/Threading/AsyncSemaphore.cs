namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步信号。
/// </summary>
internal sealed class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(true);
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private int _currentCount;

    public AsyncSemaphore(int initialCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(initialCount);
        _currentCount = initialCount;
    }

    /// <summary>
    /// 异步等待
    /// </summary>
    /// <returns></returns>
    public Task WaitAsync()
    {
        lock (_waiters)
        {
            if (_currentCount > 0)
            {
                _currentCount--;
                return _completed;
            }
            var taskCompletionSource = new TaskCompletionSource<bool>();
            _waiters.Enqueue(taskCompletionSource);
            return taskCompletionSource.Task;
        }
    }

    /// <summary>
    /// 释放
    /// </summary>
    public void Release()
    {
        TaskCompletionSource<bool> taskCompletionSource = null!;
        lock (_waiters)
        {
            if (_waiters.Count > 0)
            {
                taskCompletionSource = _waiters.Dequeue();
            }
            else
            {
                _currentCount++;
            }
        }
        taskCompletionSource?.SetResult(true);
    }
}