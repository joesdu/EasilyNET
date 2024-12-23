using System.Collections.Concurrent;

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">Asynchronous semaphore</para>
///     <para xml:lang="zh">异步信号</para>
/// </summary>
internal sealed class AsyncSemaphore
{
    private static readonly Task _completed = Task.FromResult(true);
    private readonly ConcurrentQueue<TaskCompletionSource<bool>> _waiters = new();
    private int _isTaken;

    /// <summary>
    ///     <para xml:lang="en">Gets whether the semaphore is taken</para>
    ///     <para xml:lang="zh">获取是否被占用</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The taken status of the semaphore</para>
    ///     <para xml:lang="zh">信号量的占用状态</para>
    /// </returns>
    public int GetTaken() => _isTaken;

    /// <summary>
    ///     <para xml:lang="en">Gets the number of tasks waiting</para>
    ///     <para xml:lang="zh">获取等待的任务数量</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">The number of tasks waiting</para>
    ///     <para xml:lang="zh">等待的任务数量</para>
    /// </returns>
    public int GetQueueCount() => _waiters.Count;

    /// <summary>
    ///     <para xml:lang="en">Waits asynchronously</para>
    ///     <para xml:lang="zh">异步等待</para>
    /// </summary>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation</para>
    ///     <para xml:lang="zh">表示异步操作的任务</para>
    /// </returns>
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
    ///     <para xml:lang="en">Releases the semaphore</para>
    ///     <para xml:lang="zh">释放信号量</para>
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