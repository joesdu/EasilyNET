using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">
///     Provides an asynchronous mutual exclusion lock. This lock is non-reentrant.
///     It uses a custom lightweight FIFO waiter queue and a fast path via Interlocked for high performance.
///     </para>
///     <para xml:lang="zh">
///     提供一个异步互斥锁。此锁不可重入。
///     通过 Interlocked 快路径与自定义的 FIFO 等待队列实现高性能异步锁。
///     </para>
/// </summary>
[DebuggerDisplay("IsHeld = {IsHeld}, Waiting = {WaitingCount}")]
public sealed class AsyncLock : IDisposable
{
    private readonly Task<Release> _cachedReleaseTask;

    // Waiters are stored in a linked list to support O(1) removal on cancellation.
    private readonly Lock _sync = new();
    private readonly LinkedList<Waiter> _waiters = [];

    private bool _disposed;

    // 0 = free, 1 = held
    private int _state;
    private int _waiterCount; // updated under lock and read using Volatile.Read

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="AsyncLock" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="AsyncLock" /> 类的新实例。</para>
    /// </summary>
    public AsyncLock()
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
            ObjectDisposedException.ThrowIf(_disposed, this);
            return Volatile.Read(ref _state) != 0;
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
    public int WaitingCount => _disposed ? 0 : Volatile.Read(ref _waiterCount);

    /// <summary>
    ///     <para xml:lang="en">Releases the resources used by the <see cref="AsyncLock" />.</para>
    ///     <para xml:lang="zh">释放 <see cref="AsyncLock" /> 使用的资源。</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        List<Waiter>? toCancel = null;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_waiters.Count > 0)
            {
                toCancel = new(_waiters.Count);
                for (var node = _waiters.First; node is not null; node = node.Next)
                {
                    toCancel.Add(node.Value);
                }
                _waiters.Clear();
                _waiterCount = 0;
            }
            // Mark as not held to allow GC; current holder can still call Release which will be a no-op for waiters.
            Volatile.Write(ref _state, 0);
        }
        if (toCancel is null)
        {
            return;
        }
        foreach (var w in toCancel)
        {
            w.CancellationRegistration.Dispose();
            w.Tcs.TrySetException(new ObjectDisposedException(nameof(AsyncLock)));
        }
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
    /// <exception cref="ObjectDisposedException">The <see cref="AsyncLock" /> has been disposed。</exception>
    public Task<Release> LockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        // Fast uncontended path: try to set state from 0 -> 1.
        if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
        {
            return _cachedReleaseTask;
        }

        // Contended path: enqueue a waiter.
        var waiter = new Waiter(this);

        // Cancellation registration (installed before enqueue to avoid missed cancellation windows).
        if (cancellationToken.CanBeCanceled)
        {
            waiter.CancellationRegistration = cancellationToken.Register(static s =>
            {
            waiter.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
                w.TryCancel();
            }, waiter);
            if (cancellationToken.IsCancellationRequested)
            {
                // Ensure immediate cancellation observes the token properly.
                waiter.CancellationRegistration.Dispose();
                waiter.TryCancel();
                return waiter.Tcs.Task;
            }
        }
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            waiter.Node = _waiters.AddLast(waiter);
            _waiterCount++;
        }
        return waiter.Tcs.Task;
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
    /// <exception cref="ObjectDisposedException">The <see cref="AsyncLock" /> has been disposed。</exception>
    public async Task LockAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        using (await LockAsync(cancellationToken).ConfigureAwait(false))
        {
            await action().ConfigureAwait(false);
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
    /// <exception cref="ObjectDisposedException">The <see cref="AsyncLock" /> has been disposed。</exception>
    public async Task<TResult> LockAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        using (await LockAsync(cancellationToken).ConfigureAwait(false))
        {
            return await func().ConfigureAwait(false);
        }
    }

    private void ReleaseInternal()
    {
        Waiter? next = null;
        lock (_sync)
        {
            if (_disposed)
            {
                // If disposed, just mark as not held.
                Volatile.Write(ref _state, 0);
                return;
            }
            if (_waiters.Count > 0)
            {
                next = _waiters.First!.Value;
                _waiters.RemoveFirst();
                next.Node = null;
                _waiterCount--;
                // Keep _state held (1) since ownership transfers to the next waiter.
            }
            else
            {
                // No one waiting, mark free.
                Volatile.Write(ref _state, 0);
            }
        }
        if (next is null)
        {
            return;
        }
        next.CancellationRegistration.Dispose();
        // Continue with a fresh Release instance for the next owner.
        next.Tcs.TrySetResult(new(this));
    }

    /// <summary>
    ///     <para xml:lang="en">A disposable structure that releases the <see cref="AsyncLock" /> when disposed.</para>
    ///     <para xml:lang="zh">一个可释放的结构，在释放时解除 <see cref="AsyncLock" />。</para>
    /// </summary>
    public readonly struct Release : IDisposable
    {
        private readonly AsyncLock? _asyncLockToRelease;

        internal Release(AsyncLock? asyncLockToRelease)
        {
            _asyncLockToRelease = asyncLockToRelease;
        }

        /// <summary>
        ///     <para xml:lang="en">Releases the <see cref="AsyncLock" />。</para>
        ///     <para xml:lang="zh">释放 <see cref="AsyncLock" />。</para>
        /// </summary>
        public void Dispose()
        {
            _asyncLockToRelease?.ReleaseInternal();
        }
    }

    private sealed class Waiter
    {
        private readonly AsyncLock _owner;
        internal readonly TaskCompletionSource<Release> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal CancellationTokenRegistration CancellationRegistration;
        internal LinkedListNode<Waiter>? Node;

        internal Waiter(AsyncLock owner)
        {
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryCancel()
        {
            var removed = false;
            lock (_owner._sync)
            {
                if (Node is not null)
                {
                    _owner._waiters.Remove(Node);
                    Node = null;
                    _owner._waiterCount--;
                    removed = true;
                }
            }
            if (removed)
            {
                // Propagate cancellation
                Tcs.TrySetCanceled();
            }
        }
    }
}