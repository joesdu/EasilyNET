// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">
///     Asynchronous lock, used to allow only one thread to access shared resources. When you need to implement scenarios that limit
///     concurrent access, you can use .NET's built-in <see cref="SemaphoreSlim" />.
///     </para>
///     <para xml:lang="zh">异步锁，用于仅让一个线程访问共享资源。当需要实现限制并发访问的场景时，可以使用 .NET 自带的 <see cref="SemaphoreSlim" />。</para>
/// </summary>
public sealed class AsyncLock
{
    private readonly Task<Release> _release;
    private readonly AsyncSemaphore _semaphore = new();

    /// <summary>
    ///     <para xml:lang="en">Constructor</para>
    ///     <para xml:lang="zh">构造函数</para>
    /// </summary>
    public AsyncLock()
    {
        _release = Task.FromResult(new Release(this));
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the occupancy status of the internal semaphore</para>
    ///     <para xml:lang="zh">获取内部信号量的占用状态</para>
    /// </summary>
    public int GetSemaphoreTaken() => _semaphore.GetTaken();

    /// <summary>
    ///     <para xml:lang="en">Gets the queue count of the internal semaphore</para>
    ///     <para xml:lang="zh">获取内部信号量的队列计数</para>
    /// </summary>
    public int GetQueueCount() => _semaphore.GetQueueCount();

    /// <summary>
    ///     <para xml:lang="en">Locks and returns a <see cref="Release" /> object</para>
    ///     <para xml:lang="zh">锁定，返回一个 <see cref="Release" /> 对象</para>
    /// </summary>
    public Task<Release> LockAsync()
    {
        var task = _semaphore.WaitAsync();
        return task.IsCompleted
                   ? _release
                   : task.ContinueWith((_, state) =>
                       new Release((AsyncLock?)state), this, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Current);
    }

    /// <summary>
    ///     <para xml:lang="en">Locks the execution of a task, without returning a value</para>
    ///     <para xml:lang="zh">锁定任务的执行，无返回值</para>
    /// </summary>
    /// <param name="taskFunc">
    ///     <para xml:lang="en">The task function to execute</para>
    ///     <para xml:lang="zh">要执行的任务函数</para>
    /// </param>
    public async Task LockAsync(Func<Task> taskFunc)
    {
        using (await LockAsync())
        {
            await taskFunc();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Locks the execution of a task and returns the result of the executed function</para>
    ///     <para xml:lang="zh">锁定任务的执行，并返回执行函数的结果</para>
    /// </summary>
    /// <param name="taskFunc">
    ///     <para xml:lang="en">The task function to execute</para>
    ///     <para xml:lang="zh">要执行的任务函数</para>
    /// </param>
    public async Task<T> LockAsync<T>(Func<Task<T>> taskFunc)
    {
        using (await LockAsync())
        {
            return await taskFunc();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Release</para>
    ///     <para xml:lang="zh">释放</para>
    /// </summary>
    /// <param name="asyncLock">
    ///     <para xml:lang="en">The async lock</para>
    ///     <para xml:lang="zh">异步锁</para>
    /// </param>
    public readonly struct Release(AsyncLock? asyncLock) : IDisposable
    {
        /// <inheritdoc />
        public void Dispose()
        {
            asyncLock?._semaphore.Release();
        }
    }
}