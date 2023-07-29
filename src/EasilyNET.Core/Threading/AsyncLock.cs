namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步锁
/// </summary>
public class AsyncLock
{
    /// <summary>
    /// 释放者
    /// </summary>
    public struct Release : IDisposable
    {
        private readonly AsyncLock _asyncLock;

        /// <summary>
        /// 释放者
        /// </summary>
        /// <param name="asyncLock"></param>
        public Release(AsyncLock asyncLock)
        {
            _asyncLock = asyncLock;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_asyncLock != null)
            {
                _asyncLock._semaphore.Release();
            }
        }
    }
    
    private readonly AsyncSemaphore _semaphore;
    private readonly Task<Release> _release;



    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncLock()
    {

        _semaphore = new(1);
        _release = Task.FromResult(new Release(this));
    }


    /// <summary>
    /// 锁定，返回一个 <see cref="Release"/> 对象。
    /// </summary>
    /// <returns></returns>
    public Task<Release> LockAsync()
    {
       var task= _semaphore.WaitAsync();
       return !task.IsCompleted ? task.ContinueWith((_, state) => new Release((AsyncLock)state!), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default) : _release;
    }
    
    /// <summary>
    /// 锁定任务的执行。
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task LockAsync(Task task)
    {
        using var releaser = LockAsync();
        await task;
    }
    
}


/// <summary>
/// 异步信号。
/// </summary>
internal class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(result: true);
    private readonly Queue<TaskCompletionSource<bool>> _waiters = new();
    private int _currentCount;

    public AsyncSemaphore(int initialCount)
    {
        if (initialCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCount));
        }

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

            TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
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

        taskCompletionSource?.SetResult(result: true);
    }
}