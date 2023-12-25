namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步锁
/// </summary>
public sealed class AsyncLock
{
    private readonly Task<Release> _release;

    private readonly AsyncSemaphore _semaphore;

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncLock()
    {
        _semaphore = new(1);
        _release = Task.FromResult(new Release(this));
    }

    /// <summary>
    /// 锁定，返回一个 <see cref="Release" /> 对象。
    /// </summary>
    /// <returns></returns>
    public Task<Release> LockAsync()
    {
        var task = _semaphore.WaitAsync();
        return !task.IsCompleted ? task.ContinueWith((_, state) => new Release((AsyncLock)state!), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default) : _release;
    }

    /// <summary>
    /// 锁定任务的执行。
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task LockAsync(Task task)
    {
        using var r = LockAsync();
        await task;
    }

    /// <remarks>
    /// 释放者
    /// </remarks>
    /// <param name="asyncLock"></param>
    public readonly struct Release(AsyncLock asyncLock) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose() => asyncLock._semaphore.Release();
    }
}