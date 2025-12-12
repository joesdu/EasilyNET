using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedMember.Global

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

    private volatile bool _disposed;

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
        // ReSharper disable once InvertIf
        if (toCancel is not null)
        {
            foreach (var w in toCancel)
            {
                w.CancellationRegistration.Dispose();
                w.Tcs.TrySetException(new ObjectDisposedException(nameof(AsyncLock), "AsyncLock has been disposed"));
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire the lock immediately.</para>
    ///     <para xml:lang="zh">立即尝试同步获取锁。</para>
    /// </summary>
    public bool TryWait(out Release releaser) => TryLock(TimeSpan.Zero, out releaser);

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire the lock within the specified timeout and observes cancellation.</para>
    ///     <para xml:lang="zh">在指定超时时间内尝试同步获取锁（可取消）。</para>
    /// </summary>
    public bool TryWait(TimeSpan timeout, out Release releaser, CancellationToken cancellationToken) => TryLockWithCancellation(timeout, out releaser, cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Asynchronously waits to acquire the lock (alias of LockAsync).</para>
    ///     <para xml:lang="zh">异步等待获取锁（LockAsync 的别名）。</para>
    /// </summary>
    public Task<Release> WaitAsync(CancellationToken cancellationToken = default) => LockAsync(cancellationToken);

    /// <summary>
    ///     <para xml:lang="en">Asynchronously waits to acquire the lock with timeout.</para>
    ///     <para xml:lang="zh">带超时的异步等待获取锁。</para>
    /// </summary>
    public async Task<(bool Acquired, Release Releaser)> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            var r = await LockAsync(cts.Token).ConfigureAwait(false);
            return (true, r);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, default);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire the lock within the specified timeout.</para>
    ///     <para xml:lang="zh">在指定超时时间内尝试同步获取锁。</para>
    /// </summary>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool TryLock(TimeSpan timeout, out Release releaser, CancellationToken cancellationToken = default) => TryLockWithCancellation(timeout, out releaser, cancellationToken);

    private bool TryLockWithCancellation(TimeSpan timeout, out Release releaser, CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        // If caller already canceled, honor it.
        cancellationToken.ThrowIfCancellationRequested();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            releaser = LockAsync(cts.Token).GetAwaiter().GetResult();
            return true;
        }
        catch (OperationCanceledException)
        {
            // Distinguish external cancellation from timeout.
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            releaser = default;
            return false; // timeout
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
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Release>(cancellationToken);
        }

        // Fast uncontended path: try to set state from 0 -> 1.
        if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
        {
            return _cachedReleaseTask;
        }

        // Contended path: enqueue a waiter.
        var waiter = new Waiter(this) { CancellationToken = cancellationToken };

        // Cancellation registration (installed before enqueue to avoid missed cancellation windows).
        if (cancellationToken.CanBeCanceled)
        {
            // 使用 UnsafeRegister，避免流动 ExecutionContext，提高性能
            waiter.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var w = (Waiter?)s;
                w?.TryCancel();
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

            // If already canceled, avoid enqueue
            if (waiter.Tcs.Task.IsCompleted)
            {
                return waiter.Tcs.Task;
            }

            // Re-check state under lock; if the lock became free between fast path and now, try to acquire atomically
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                // cleanup registration to avoid leaks since we won't use this waiter
                waiter.CancellationRegistration.Dispose();
                return _cachedReleaseTask;
            }
            waiter.Node = _waiters.AddLast(waiter);
            _waiterCount++;
        }
        return waiter.Tcs.Task;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock, executes an asynchronous action, and then releases the lock.</para>
    ///     <para xml:lang="zh">异步获取锁，执行一个异步操作，然后释放锁。</para>
    /// </summary>
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
        while (true)
        {
            Waiter? next;
            lock (_sync)
            {
                if (_disposed)
                {
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
                    Volatile.Write(ref _state, 0);
                    return;
                }
            }
            next.CancellationRegistration.Dispose();
            if (next.Tcs.TrySetResult(new(this)))
            {
                // Ownership transferred to next waiter, availability stays reset.
                return;
            }
            // Otherwise, loop to pick another waiter or release if none.
            // If the selected waiter was already canceled/completed, retry the handoff.
        }
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
        internal CancellationToken CancellationToken;
        internal LinkedListNode<Waiter>? Node;

        internal Waiter(AsyncLock owner)
        {
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryCancel()
        {
            lock (_owner._sync)
            {
                if (Node is not null)
                {
                    _owner._waiters.Remove(Node);
                    Node = null;
                    _owner._waiterCount--;
                }
            }
            CancellationRegistration.Dispose();
            // Always complete as canceled so producers can observe and avoid enqueuing or awaiting forever.
            Tcs.TrySetCanceled(CancellationToken);
        }
    }
}