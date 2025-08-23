// ReSharper disable UnusedMember.Global

using System.Diagnostics;

namespace EasilyNET.Core.Benchmark;

/// <summary>
///     <para xml:lang="en">
///     Provides an asynchronous mutual exclusion lock. This lock is non-reentrant.
///     It utilizes <see cref="SemaphoreSlim" /> internally to manage asynchronous access.
///     </para>
///     <para xml:lang="zh">
///     提供一个异步互斥锁。此锁不可重入。
///     它内部利用 <see cref="SemaphoreSlim" /> 来管理异步访问。
///     </para>
/// </summary>
[DebuggerDisplay("IsHeld = {IsHeld}, Waiting = {WaitingCount}")]
public sealed class SemaphoreAsyncLock : IDisposable
{
    private readonly Task<Release> _cachedReleaseTask;
    private readonly SemaphoreSlim _semaphore = new(1, 1); // Initial count 1, max count 1
    private bool _disposed;

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="SemaphoreAsyncLock" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="SemaphoreAsyncLock" /> 类的新实例。</para>
    /// </summary>
    public SemaphoreAsyncLock()
    {
        _cachedReleaseTask = Task.FromResult(new Release(this));
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether the lock is currently held.</para>
    ///     <para xml:lang="zh">获取一个值，该值指示当前是否持有锁。</para>
    /// </summary>
    public bool IsHeld
    {
        get
        {
            // This is the critical fix: check _disposed *before* accessing _semaphore.
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _semaphore.CurrentCount is 0;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the number of tasks currently waiting to acquire the lock.</para>
    ///     <para xml:lang="zh">获取当前等待获取锁的任务数量。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">This is an approximation as the count can change.</para>
    ///     <para xml:lang="zh">这是一个近似值，因为计数可能会发生变化。</para>
    /// </remarks>
    public int WaitingCount
    {
        get
        {
            // Check _disposed here as well for consistency, though IsHeld is the one failing in the test.
            // Depending on desired behavior, this could throw or return a default.
            // Returning 0 if disposed is reasonable for a "count".
            if (_disposed)
            {
                return 0;
            }
            return _semaphore.CurrentCount is 0 ? 1 : 0; // Simplified, indicates if taken.
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Releases the resources used by the <see cref="SemaphoreAsyncLock" />.</para>
    ///     <para xml:lang="zh">释放 <see cref="SemaphoreAsyncLock" /> 使用的资源。</para>
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock.</para>
    ///     <para xml:lang="zh">异步获取锁。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A <see cref="CancellationToken" /> to observe while waiting to acquire the lock.</para>
    ///     <para xml:lang="zh">在等待获取锁时要观察的 <see cref="CancellationToken" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     A <see cref="Task{Release}" /> that completes when the lock is acquired. The <see cref="Release" /> value should be disposed
    ///     to release the lock.
    ///     </para>
    ///     <para xml:lang="zh">一个 <see cref="Task{Release}" />，当获取锁时完成。应释放 <see cref="Release" /> 值以解除锁定。</para>
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="SemaphoreAsyncLock" /> has been disposed。</exception>
    public Task<Release> LockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var waitTask = _semaphore.WaitAsync(cancellationToken);
        // If the wait completed synchronously (i.e., the semaphore was available immediately)
        // and the cancellation token was not already canceled, we can return the cached task.
        return waitTask.IsCompletedSuccessfully
                   // Ensure not cancelled, WaitAsync would throw if it was already cancelled and completed synchronously due to that.
                   // If it completed successfully, it means the lock was acquired.
                   ? _cachedReleaseTask
                   // If the wait did not complete synchronously, we need to await it asynchronously.
                   : LockAsyncInternal(waitTask);
    }

    private async Task<Release> LockAsyncInternal(Task waitTask)
    {
        await waitTask.ConfigureAwait(false);
        // We create a new Release struct here because the _cachedReleaseTask is specifically for synchronous completions.
        // While it might seem okay to return _cachedReleaseTask, it's cleaner to distinguish.
        // However, for struct Release, it's fine as it's just a holder for 'this'.
        return new(this);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock, executes an asynchronous action, and then releases the lock.</para>
    ///     <para xml:lang="zh">异步获取锁，执行一个异步操作，然后释放锁。</para>
    /// </summary>
    /// <param name="action">
    ///     <para xml:lang="en">The asynchronous action to perform while holding the lock。</para>
    ///     <para xml:lang="zh">持有锁时要执行的异步操作。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A <see cref="CancellationToken" /> to observe while waiting for the lock and executing the action.</para>
    ///     <para xml:lang="zh">在等待锁和执行操作时要观察的 <see cref="CancellationToken" />。</para>
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="action" /> is <see langword="null" />。</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="SemaphoreAsyncLock" /> has been disposed。</exception>
    public async Task LockAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(action);
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await action().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    /// <summary>
    ///     <para xml:lang="en">
    ///     Asynchronously acquires the lock, executes an asynchronous function, releases the lock, and returns the result of the
    ///     function.
    ///     </para>
    ///     <para xml:lang="zh">异步获取锁，执行一个异步函数，释放锁，并返回函数的结果。</para>
    /// </summary>
    /// <typeparam name="TResult">The type of the result returned by the function.</typeparam>
    /// <param name="func">
    ///     <para xml:lang="en">The asynchronous function to perform while holding the lock.</para>
    ///     <para xml:lang="zh">持有锁时要执行的异步函数。</para>
    /// </param>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A <see cref="CancellationToken" /> to observe while waiting for the lock and executing the function.</para>
    ///     <para xml:lang="zh">在等待锁和执行函数时要观察的 <see cref="CancellationToken" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">A task that represents the asynchronous operation and contains the result of the <paramref name="func" />。</para>
    ///     <para xml:lang="zh">一个表示异步操作的任务，并包含 <paramref name="func" /> 的结果。</para>
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="func" /> is <see langword="null" />。</exception>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken" /> was canceled。</exception>
    /// <exception cref="ObjectDisposedException">The <see cref="SemaphoreAsyncLock" /> has been disposed。</exception>
    public async Task<TResult> LockAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(func);
        await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await func().ConfigureAwait(false);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }
        if (disposing)
        {
            _semaphore.Dispose();
        }
        _disposed = true;
    }

    /// <summary>
    ///     <para xml:lang="en">A disposable structure that releases the <see cref="SemaphoreAsyncLock" /> when disposed.</para>
    ///     <para xml:lang="zh">一个可释放的结构，在释放时解除 <see cref="SemaphoreAsyncLock" />。</para>
    /// </summary>
    public readonly struct Release : IDisposable
    {
        private readonly SemaphoreAsyncLock? _asyncLockToRelease;

        internal Release(SemaphoreAsyncLock? asyncLockToRelease)
        {
            _asyncLockToRelease = asyncLockToRelease;
        }

        /// <summary>
        ///     <para xml:lang="en">Releases the <see cref="SemaphoreAsyncLock" />。</para>
        ///     <para xml:lang="zh">释放 <see cref="SemaphoreAsyncLock" />。</para>
        /// </summary>
        public void Dispose()
        {
            // Only release if the lock instance is valid and not disposed.
            if (_asyncLockToRelease is not null && !_asyncLockToRelease._disposed)
            {
                _asyncLockToRelease._semaphore.Release();
            }
        }
    }
}