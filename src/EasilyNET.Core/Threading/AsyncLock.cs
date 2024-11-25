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
    public Task<Release> LockAsync()
    {
        var task = _semaphore.WaitAsync();
        return task.IsCompleted
                   ? _release
                   : task.ContinueWith((_, state) =>
                       new Release((AsyncLock?)state), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
    }

    /// <summary>
    /// 锁定任务的执行,无返回值
    /// </summary>
    /// <param name="taskFunc"></param>
    /// <returns></returns>
    public async Task LockAsync(Func<Task> taskFunc)
    {
        using (await LockAsync())
        {
            await taskFunc();
        }
    }

    /// <summary>
    /// 锁定任务的执行,可返回执行函数的结果
    /// </summary>
    /// <param name="taskFunc"></param>
    /// <returns></returns>
    // ReSharper disable once UnusedMethodReturnValue.Global
    public async Task<T> LockAsync<T>(Func<Task<T>> taskFunc)
    {
        using (await LockAsync())
        {
            return await taskFunc();
        }
    }

    /// <remarks>
    /// Release
    /// </remarks>
    /// <param name="asyncLock"></param>
    public readonly struct Release(AsyncLock? asyncLock) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            asyncLock?._semaphore.Release();
        }
    }
}