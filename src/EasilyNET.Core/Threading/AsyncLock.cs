using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable MemberCanBePrivate.Global

// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">
///     Provides an asynchronous mutual exclusion lock with optional reentrancy.
///     Uses a lightweight FIFO waiter queue and Interlocked fast path for high performance.
///     </para>
///     <para xml:lang="zh">
///     提供一个支持可选重入的异步互斥锁，
///     通过轻量级 FIFO 等待队列和 Interlocked 快路径实现高性能。
///     </para>
/// </summary>
[DebuggerDisplay("IsHeld = {IsHeld}, Waiting = {WaitingCount}")]
public sealed class AsyncLock : IDisposable
{
    private readonly bool _allowReentrancy;
    private readonly Task<Release> _cachedReleaseTask;
    private readonly AsyncLocal<Guid> _ownerContext = new();
    private readonly Lock _sync = new();
    private readonly SimpleObjectPool<TaskCompletionSource<Release>> _tcsPool;
    private readonly LinkedList<Waiter> _waiters = [];
    private bool _disposed;

    // 0 = free, 1 = held
    private int _state;
    private int _waiterCount; // updated under lock and read using Volatile.Read

    /// <summary>
    ///     <para xml:lang="en">Initializes a new instance of the <see cref="AsyncLock" /> class.</para>
    ///     <para xml:lang="zh">初始化 <see cref="AsyncLock" /> 类的新实例。</para>
    /// </summary>
    /// <param name="allowReentrancy">
    ///     <para xml:lang="en">Whether the lock allows reentrancy for the same context. Defaults to false (non-reentrant).</para>
    ///     <para xml:lang="zh">是否允许同一上下文重入锁。默认为 false（不可重入）。</para>
    /// </param>
    public AsyncLock(bool allowReentrancy = false)
    {
        _allowReentrancy = allowReentrancy;
        _cachedReleaseTask = Task.FromResult(new Release(this));
        _tcsPool = new(() => new(TaskCreationOptions.RunContinuationsAsynchronously));
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
            Volatile.Write(ref _state, 0);
            _ownerContext.Value = Guid.Empty;
        }
        if (toCancel is not null)
        {
            foreach (var w in toCancel)
            {
                w.CancellationRegistration.Dispose();
                w.Tcs.TrySetException(new ObjectDisposedException(nameof(AsyncLock), "AsyncLock has been disposed"));
                _tcsPool.Return(w.Tcs);
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
    ///     <para xml:lang="en">A <see cref="Task{Release}" /> that completes when the lock is acquired.</para>
    ///     <para xml:lang="zh">一个 <see cref="Task{Release}" />，当获取锁时完成。</para>
    /// </returns>
    public Task<Release> LockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_allowReentrancy && _ownerContext.Value != Guid.Empty && Volatile.Read(ref _state) == 1)
        {
            if (_ownerContext.Value != GetCurrentOwnerId())
            {
                throw new InvalidOperationException("尝试重入锁，但持有者不匹配");
            }
            cancellationToken.ThrowIfCancellationRequested(); // 新增：检查重入时取消状态
            return Task.FromResult(new Release(this, true));
        }
        if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
        {
            _ownerContext.Value = Guid.NewGuid();
            return _cachedReleaseTask;
        }
        var waiter = new Waiter(this, _tcsPool.Get());
        if (cancellationToken.CanBeCanceled)
        {
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
            if (waiter.Tcs.Task.IsCompleted)
            {
                _tcsPool.Return(waiter.Tcs);
                return waiter.Tcs.Task;
            }
            if (Interlocked.CompareExchange(ref _state, 1, 0) == 0)
            {
                waiter.CancellationRegistration.Dispose();
                _ownerContext.Value = Guid.NewGuid();
                _tcsPool.Return(waiter.Tcs);
                return _cachedReleaseTask;
            }
            waiter.Node = _waiters.AddLast(waiter);
            _waiterCount++;
            Debug.Assert(_waiterCount == _waiters.Count, "Waiter count mismatch in LockAsync");
        }
        return waiter.Tcs.Task;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock, executes an action, and releases the lock.</para>
    ///     <para xml:lang="zh">异步获取锁，执行操作，然后释放锁。</para>
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
    ///     <para xml:lang="en">Asynchronously acquires the lock, executes a function, and returns its result.</para>
    ///     <para xml:lang="zh">异步获取锁，执行函数，并返回其结果。</para>
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
                    _ownerContext.Value = Guid.Empty;
                    return;
                }
                if (_waiters.Count > 0)
                {
                    next = _waiters.First!.Value;
                    _waiters.RemoveFirst();
                    next.Node = null;
                    _waiterCount--;
                    _ownerContext.Value = Guid.NewGuid();
                    Debug.Assert(_waiterCount == _waiters.Count, "Waiter count mismatch in ReleaseInternal");
                }
                else
                {
                    Volatile.Write(ref _state, 0);
                    _ownerContext.Value = Guid.Empty;
                    return;
                }
            }
            next.CancellationRegistration.Dispose();
            if (next.Tcs.TrySetResult(new(this)))
            {
                _tcsPool.Return(next.Tcs);
                return;
            }
            if (next.Tcs.Task.IsCanceled)
            {
                _tcsPool.Return(next.Tcs);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the current owner ID for the lock context.</para>
    ///     <para xml:lang="zh">获取当前锁上下文的持有者 ID。</para>
    /// </summary>
    private Guid GetCurrentOwnerId() => _ownerContext.Value;

    /// <summary>
    ///     <para xml:lang="en">A disposable structure that releases the <see cref="AsyncLock" /> when disposed.</para>
    ///     <para xml:lang="zh">一个可释放的结构，在释放时解除 <see cref="AsyncLock" />。</para>
    /// </summary>
    public readonly struct Release : IDisposable
    {
        private readonly AsyncLock? _asyncLockToRelease;
        private readonly bool _isReentrant;

        internal Release(AsyncLock? asyncLockToRelease, bool isReentrant = false)
        {
            _asyncLockToRelease = asyncLockToRelease;
            _isReentrant = isReentrant;
        }

        /// <summary>
        ///     <para xml:lang="en">Releases the <see cref="AsyncLock" />.</para>
        ///     <para xml:lang="zh">释放 <see cref="AsyncLock" />。</para>
        /// </summary>
        public void Dispose()
        {
            if (_isReentrant)
            {
                return;
            }
            _asyncLockToRelease?.ReleaseInternal();
        }
    }

    private sealed class Waiter
    {
        private readonly AsyncLock _owner;
        internal readonly TaskCompletionSource<Release> Tcs;
        internal CancellationTokenRegistration CancellationRegistration;
        internal LinkedListNode<Waiter>? Node;

        internal Waiter(AsyncLock owner, TaskCompletionSource<Release> tcs)
        {
            _owner = owner;
            Tcs = tcs;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryCancel()
        {
            lock (_owner._sync)
            {
                if (Node is null)
                {
                    return;
                }
                _owner._waiters.Remove(Node);
                Node = null;
                _owner._waiterCount--;
                Debug.Assert(_owner._waiterCount == _owner._waiters.Count, "Waiter count mismatch in TryCancel");
                Tcs.TrySetCanceled();
                _owner._tcsPool.Return(Tcs);
            }
        }
    }

    /// <summary>
    ///     <para xml:lang="en">A thread-safe object pool for reusing objects, optimized for high-concurrency.</para>
    ///     <para xml:lang="zh">一个线程安全对象池，用于重用对象，优化高并发场景。</para>
    /// </summary>
    private sealed class SimpleObjectPool<T>(Func<T> factory) where T : class
    {
        private readonly Func<T> _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        private readonly ConcurrentBag<T> _pool = [];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Get() => _pool.TryTake(out var item) ? item : _factory();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return(T? item)
        {
            if (item != null)
            {
                _pool.Add(item);
            }
        }
    }
}