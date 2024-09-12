// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
/// 异步锁,用于仅让一个线程访问共享资源.当需要实现限制并发访问的场景时,可以使用.NET自带的 <see cref="SemaphoreSlim" />
/// </summary>
public sealed class AsyncLock
{
    private readonly Task<Release> _release;

    private readonly AsyncSemaphore _semaphore = new();

    /// <summary>
    /// 构造函数
    /// </summary>
    public AsyncLock()
    {
        _release = Task.FromResult(new Release(this));
    }

    /// <summary>
    /// </summary>
    /// <returns></returns>
    public int GetSemaphoreTaken() => _semaphore.GetTaken();

    /// <summary>
    /// 获取内部信号量的队列计数
    /// </summary>
    /// <returns></returns>
    public int GetQueueCount() => _semaphore.GetQueueCount();

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