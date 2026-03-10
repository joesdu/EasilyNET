using System.Diagnostics;
using System.Runtime.CompilerServices;

// ReSharper disable UnusedType.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace EasilyNET.Core.Threading;

/// <summary>
///     <para xml:lang="en">
///     Provides a high-performance asynchronous reader-writer lock. Multiple readers may hold the lock concurrently;
///     writers get exclusive access. This lock is non-reentrant and uses writer-preference semantics to prevent
///     writer starvation. It uses a packed <see langword="int" /> state field, Interlocked CAS fast paths,
///     and separate FIFO waiter queues for readers and writers — all matching the design of <see cref="AsyncLock" />.
///     </para>
///     <para xml:lang="zh">
///     提供一个高性能的异步读写锁。多个读者可以同时持有锁；写者需要独占访问。
///     此锁不可重入，采用写者优先策略以防止写者饥饿。
///     使用打包的 <see langword="int" /> 状态字段、Interlocked CAS 快速路径以及独立的读者/写者 FIFO 等待队列，
///     设计风格与 <see cref="AsyncLock" /> 完全一致。
///     </para>
/// </summary>
/// <remarks>
///     <para xml:lang="en">
///     <b>State encoding (single <see langword="int" /> <c>_state</c> field):</b>
///     <list type="bullet">
///         <item>Bits 0–28: active reader count (max ~536 million simultaneous readers)</item>
///         <item>Bit 29 (<c>WriterWaitingBit = 0x20000000</c>): at least one writer is queued; new readers are blocked</item>
///         <item>Bit 30 (<c>WriterHeldBit = 0x40000000</c>): a writer currently holds the lock</item>
///         <item>Bit 31: unused (sign bit kept free)</item>
///     </list>
///     Reader fast path: CAS-increment bits 0–28 when neither <c>WriterHeldBit</c> nor <c>WriterWaitingBit</c> is set.
///     Writer fast path: CAS from 0 to <c>WriterHeldBit</c>.
///     </para>
///     <para xml:lang="zh">
///     <b>状态编码（单个 <see langword="int" /> 字段 <c>_state</c>）：</b>
///     <list type="bullet">
///         <item>位 0–28：活跃读者计数（最大约 5.36 亿）</item>
///         <item>位 29 (<c>WriterWaitingBit = 0x20000000</c>)：至少一个写者在排队，新读者将被阻塞</item>
///         <item>位 30 (<c>WriterHeldBit = 0x40000000</c>)：当前有写者持有锁</item>
///         <item>位 31：未使用（符号位保持空闲）</item>
///     </list>
///     读者快路径：当 <c>WriterHeldBit</c> 和 <c>WriterWaitingBit</c> 均未置位时，CAS 自增位 0–28。
///     写者快路径：从 0 CAS 到 <c>WriterHeldBit</c>。
///     </para>
///     <para xml:lang="en">
///     <b>Writer-preference policy:</b> When a writer enqueues, <c>WriterWaitingBit</c> is set atomically so that
///     subsequent <see cref="ReadLockAsync(CancellationToken)" /> calls go to the slow path and queue rather than
///     bypassing the waiting writer. On writer release (or when the last reader exits while a writer waits),
///     pending writers are preferred over pending readers.
///     </para>
///     <para xml:lang="zh">
///     <b>写者优先策略：</b>当写者入队时，<c>WriterWaitingBit</c> 被原子置位，使后续 <see cref="ReadLockAsync(CancellationToken)" />
///     调用进入慢路径排队，而不是绕过等待的写者。当写者释放锁（或最后一个读者在写者等待时退出）时，优先唤醒等待的写者。
///     </para>
/// </remarks>
/// <example>
///     <code>
/// <![CDATA[
/// private readonly AsyncReaderWriterLock _rwLock = new();
/// 
/// // 并发读（多个 Task 可同时进入）
/// public async Task<string> ReadDataAsync(CancellationToken ct = default)
/// {
///     using (await _rwLock.ReadLockAsync(ct))
///     {
///         return _data;
///     }
/// }
/// 
/// // 独占写
/// public async Task WriteDataAsync(string value, CancellationToken ct = default)
/// {
///     using (await _rwLock.WriteLockAsync(ct))
///     {
///         _data = value;
///     }
/// }
/// ]]>
///     </code>
/// </example>
[DebuggerDisplay("{DebugState}, ReadWaiting = {_readWaiterCount}, WriteWaiting = {_writeWaiterCount}")]
public sealed class AsyncReaderWriterLock : IDisposable
{
    // ── State bit layout ──────────────────────────────────────────────────────
    // Bit 30: writer holds the lock exclusively
    private const int WriterHeldBit = 0x40000000;

    // Bit 29: at least one writer is in the write-waiter queue; blocks reader fast path
    private const int WriterWaitingBit = 0x20000000;

    // Mask isolating the reader-count in bits 0–28
    private const int ReaderCountMask = 0x1FFFFFFF;

    // Separate FIFO queues allow batch-waking all readers independently of writers.
    private readonly LinkedList<ReadWaiter> _readWaiters = [];

    // ── Synchronisation ───────────────────────────────────────────────────────
    private readonly Lock _sync = new();
    private readonly LinkedList<WriteWaiter> _writeWaiters = [];

    private volatile bool _disposed;

    // Updated under _sync; read with Volatile for lightweight diagnostics.
    private int _readWaiterCount;

    // ── Packed state ──────────────────────────────────────────────────────────
    // Layout: [ bit31=unused | bit30=WriterHeld | bit29=WriterWaiting | bits28-0=ReaderCount ]
    private int _state;
    private int _writeWaiterCount;

    // ── Diagnostics ───────────────────────────────────────────────────────────

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether any reader or writer currently holds the lock.</para>
    ///     <para xml:lang="zh">获取一个值，指示当前是否有读者或写者持有锁。</para>
    /// </summary>
    public bool IsHeld
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            // Any non-zero bits in bits 0–30 means lock is held by someone.
            return (Volatile.Read(ref _state) & (WriterHeldBit | ReaderCountMask)) != 0;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets a value indicating whether a writer currently holds the lock.</para>
    ///     <para xml:lang="zh">获取一个值，指示当前是否有写者持有锁。</para>
    /// </summary>
    public bool IsWriteHeld
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return (Volatile.Read(ref _state) & WriterHeldBit) != 0;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the number of readers currently holding the lock.</para>
    ///     <para xml:lang="zh">获取当前持有锁的读者数量。</para>
    /// </summary>
    public int ReaderCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return Volatile.Read(ref _state) & ReaderCountMask;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Gets the number of tasks currently waiting to acquire a read lock.</para>
    ///     <para xml:lang="zh">获取当前等待获取读锁的任务数量。</para>
    /// </summary>
    public int ReadWaiterCount => _disposed ? 0 : Volatile.Read(ref _readWaiterCount);

    /// <summary>
    ///     <para xml:lang="en">Gets the number of tasks currently waiting to acquire the write lock.</para>
    ///     <para xml:lang="zh">获取当前等待获取写锁的任务数量。</para>
    /// </summary>
    public int WriteWaiterCount => _disposed ? 0 : Volatile.Read(ref _writeWaiterCount);

    // Used only by DebuggerDisplay — no ObjectDisposedException guard needed.
    private string DebugState
    {
        get
        {
            var s = Volatile.Read(ref _state);
            var writerHeld = (s & WriterHeldBit) != 0;
            var writerWaiting = (s & WriterWaitingBit) != 0;
            var readers = s & ReaderCountMask;
            return writerHeld
                       ? $"WriteLocked, WriterWaiting={writerWaiting}"
                       : readers > 0
                           ? $"ReadLocked({readers}), WriterWaiting={writerWaiting}"
                           : $"Free, WriterWaiting={writerWaiting}";
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    /// <summary>
    ///     <para xml:lang="en">Releases all resources used by the <see cref="AsyncReaderWriterLock" />.</para>
    ///     <para xml:lang="zh">释放 <see cref="AsyncReaderWriterLock" /> 使用的所有资源。</para>
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        List<ReadWaiter>? readToCancel = null;
        List<WriteWaiter>? writeToCancel = null;
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_readWaiters.Count > 0)
            {
                readToCancel = new(_readWaiters.Count);
                // Null out each waiter's Node before clearing so concurrent TryCancel
                // callbacks see Node == null and skip the Remove() call safely.
                while (_readWaiters.Count > 0)
                {
                    var waiter = _readWaiters.First!.Value;
                    _readWaiters.RemoveFirst();
                    waiter.Node = null;
                    readToCancel.Add(waiter);
                }
                _readWaiterCount = 0;
            }
            if (_writeWaiters.Count > 0)
            {
                writeToCancel = new(_writeWaiters.Count);
                while (_writeWaiters.Count > 0)
                {
                    var waiter = _writeWaiters.First!.Value;
                    _writeWaiters.RemoveFirst();
                    waiter.Node = null;
                    writeToCancel.Add(waiter);
                }
                _writeWaiterCount = 0;
            }
            Volatile.Write(ref _state, 0);
        }
        var ex = new ObjectDisposedException(nameof(AsyncReaderWriterLock), "AsyncReaderWriterLock has been disposed.");
        if (readToCancel is not null)
        {
            foreach (var w in readToCancel)
            {
                w.CancellationRegistration.Dispose();
                w.Tcs.TrySetException(ex);
            }
        }
        // ReSharper disable once InvertIf
        if (writeToCancel is not null)
        {
            foreach (var w in writeToCancel)
            {
                w.CancellationRegistration.Dispose();
                w.Tcs.TrySetException(ex);
            }
        }
    }

    // ── Read-lock API ─────────────────────────────────────────────────────────

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire a read lock immediately without waiting.</para>
    ///     <para xml:lang="zh">立即尝试同步获取读锁，不等待。</para>
    /// </summary>
    public bool TryReadLock(out ReadRelease releaser)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var current = Volatile.Read(ref _state);
        // Only allow when no writer holds and no writer is waiting.
        if ((current & (WriterHeldBit | WriterWaitingBit)) == 0)
        {
            if (Interlocked.CompareExchange(ref _state, current + 1, current) == current)
            {
                releaser = new(this);
                return true;
            }
        }
        releaser = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock in read mode.</para>
    ///     <para xml:lang="zh">以读模式异步获取锁。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A <see cref="CancellationToken" /> to observe while waiting to acquire the lock.</para>
    ///     <para xml:lang="zh">在等待获取锁时要观察的 <see cref="CancellationToken" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     A <see cref="ValueTask{ReadRelease}" /> that completes when the read lock is acquired.
    ///     Dispose the returned <see cref="ReadRelease" /> to release the lock.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     一个 <see cref="ValueTask{ReadRelease}" />，当读锁获取时完成。
    ///     释放返回的 <see cref="ReadRelease" /> 以解锁。
    ///     </para>
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<ReadRelease> ReadLockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<ReadRelease>(cancellationToken);
        }

        // Fast path: CAS-increment reader count when no writer holds and no writer is queued.
        // Loop because multiple readers may race on the same state value.
        var current = Volatile.Read(ref _state);
        while ((current & (WriterHeldBit | WriterWaitingBit)) == 0)
        {
            if (Interlocked.CompareExchange(ref _state, current + 1, current) == current)
            {
                return new(new ReadRelease(this));
            }
            // State changed; refresh and retry.
            current = Volatile.Read(ref _state);
        }
        return ReadLockAsyncSlow(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires a read lock with a timeout.</para>
    ///     <para xml:lang="zh">带超时的异步获取读锁。</para>
    /// </summary>
    public async Task<(bool Acquired, ReadRelease Releaser)> ReadLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            var r = await ReadLockAsync(cts.Token).ConfigureAwait(false);
            return (true, r);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, default);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires a read lock, executes an asynchronous action, then releases the lock.</para>
    ///     <para xml:lang="zh">异步获取读锁，执行异步操作，然后释放锁。</para>
    /// </summary>
    public async Task ReadLockAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        using (await ReadLockAsync(cancellationToken).ConfigureAwait(false))
        {
            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires a read lock, executes an asynchronous function, releases the lock, and returns the result.</para>
    ///     <para xml:lang="zh">异步获取读锁，执行异步函数，释放锁，并返回结果。</para>
    /// </summary>
    public async Task<TResult> ReadLockAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        using (await ReadLockAsync(cancellationToken).ConfigureAwait(false))
        {
            return await func().ConfigureAwait(false);
        }
    }

    // ── Write-lock API ────────────────────────────────────────────────────────

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire the write lock immediately without waiting.</para>
    ///     <para xml:lang="zh">立即尝试同步获取写锁，不等待。</para>
    /// </summary>
    public bool TryWriteLock(out WriteRelease releaser)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (Interlocked.CompareExchange(ref _state, WriterHeldBit, 0) == 0)
        {
            releaser = new(this);
            return true;
        }
        releaser = default;
        return false;
    }

    /// <summary>
    ///     <para xml:lang="en">Tries to synchronously acquire the write lock within the specified timeout and observes cancellation.</para>
    ///     <para xml:lang="zh">在指定超时时间内尝试同步获取写锁（可取消）。</para>
    /// </summary>
    /// <remarks>
    ///     <para xml:lang="en">WARNING: This method blocks the calling thread. Use <see cref="WriteLockAsync(CancellationToken)" /> in async contexts.</para>
    ///     <para xml:lang="zh">警告：此方法会阻塞调用线程。异步上下文中请使用 <see cref="WriteLockAsync(CancellationToken)" />。</para>
    /// </remarks>
    public bool TryWriteLock(TimeSpan timeout, out WriteRelease releaser, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        cancellationToken.ThrowIfCancellationRequested();
        if (Interlocked.CompareExchange(ref _state, WriterHeldBit, 0) == 0)
        {
            releaser = new(this);
            return true;
        }
        if (timeout == TimeSpan.Zero)
        {
            releaser = default;
            return false;
        }
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            releaser = WriteLockAsync(cts.Token).AsTask().GetAwaiter().GetResult();
            return true;
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            releaser = default;
            return false;
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the lock in write mode (exclusive).</para>
    ///     <para xml:lang="zh">以写模式（独占）异步获取锁。</para>
    /// </summary>
    /// <param name="cancellationToken">
    ///     <para xml:lang="en">A <see cref="CancellationToken" /> to observe while waiting to acquire the lock.</para>
    ///     <para xml:lang="zh">在等待获取锁时要观察的 <see cref="CancellationToken" />。</para>
    /// </param>
    /// <returns>
    ///     <para xml:lang="en">
    ///     A <see cref="ValueTask{WriteRelease}" /> that completes when the write lock is acquired.
    ///     Dispose the returned <see cref="WriteRelease" /> to release the lock.
    ///     </para>
    ///     <para xml:lang="zh">
    ///     一个 <see cref="ValueTask{WriteRelease}" />，当写锁获取时完成。
    ///     释放返回的 <see cref="WriteRelease" /> 以解锁。
    ///     </para>
    /// </returns>
    /// <exception cref="OperationCanceledException">The <paramref name="cancellationToken" /> was canceled.</exception>
    /// <exception cref="ObjectDisposedException">The lock has been disposed.</exception>
    public ValueTask<WriteRelease> WriteLockAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (cancellationToken.IsCancellationRequested)
        {
            return ValueTask.FromCanceled<WriteRelease>(cancellationToken);
        }
        // Fast path: CAS from fully-free (0) → WriterHeldBit.
        return Interlocked.CompareExchange(ref _state, WriterHeldBit, 0) == 0 ? new(new WriteRelease(this)) : WriteLockAsyncSlow(cancellationToken);
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the write lock with a timeout.</para>
    ///     <para xml:lang="zh">带超时的异步获取写锁。</para>
    /// </summary>
    public async Task<(bool Acquired, WriteRelease Releaser)> WriteLockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            var r = await WriteLockAsync(cts.Token).ConfigureAwait(false);
            return (true, r);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (false, default);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the write lock, executes an asynchronous action, then releases the lock.</para>
    ///     <para xml:lang="zh">异步获取写锁，执行异步操作，然后释放锁。</para>
    /// </summary>
    public async Task WriteLockAsync(Func<Task> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        using (await WriteLockAsync(cancellationToken).ConfigureAwait(false))
        {
            await action().ConfigureAwait(false);
        }
    }

    /// <summary>
    ///     <para xml:lang="en">Asynchronously acquires the write lock, executes an asynchronous function, releases the lock, and returns the result.</para>
    ///     <para xml:lang="zh">异步获取写锁，执行异步函数，释放锁，并返回结果。</para>
    /// </summary>
    public async Task<TResult> WriteLockAsync<TResult>(Func<Task<TResult>> func, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(func);
        using (await WriteLockAsync(cancellationToken).ConfigureAwait(false))
        {
            return await func().ConfigureAwait(false);
        }
    }

    // ── Slow paths ────────────────────────────────────────────────────────────

    private ValueTask<ReadRelease> ReadLockAsyncSlow(CancellationToken cancellationToken)
    {
        var waiter = new ReadWaiter(this) { CancellationToken = cancellationToken };
        if (cancellationToken.CanBeCanceled)
        {
            // UnsafeRegister avoids capturing ExecutionContext — matches AsyncLock pattern.
            waiter.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var w = (ReadWaiter?)s;
                w?.TryCancel();
            }, waiter);
            if (cancellationToken.IsCancellationRequested)
            {
                waiter.CancellationRegistration.Dispose();
                waiter.TryCancel();
                return new(waiter.Tcs.Task);
            }
        }
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (waiter.Tcs.Task.IsCompleted)
            {
                return new(waiter.Tcs.Task);
            }

            // Double-check: re-attempt fast path under lock in case the writer just left.
            var current = Volatile.Read(ref _state);
            while ((current & (WriterHeldBit | WriterWaitingBit)) == 0)
            {
                var next = current + 1;
                var observed = Interlocked.CompareExchange(ref _state, next, current);
                if (observed == current)
                {
                    waiter.CancellationRegistration.Dispose();
                    return new(new ReadRelease(this));
                }
                current = observed;
            }
            waiter.Node = _readWaiters.AddLast(waiter);
            _readWaiterCount++;
        }
        return new(waiter.Tcs.Task);
    }

    private ValueTask<WriteRelease> WriteLockAsyncSlow(CancellationToken cancellationToken)
    {
        var waiter = new WriteWaiter(this) { CancellationToken = cancellationToken };
        if (cancellationToken.CanBeCanceled)
        {
            waiter.CancellationRegistration = cancellationToken.UnsafeRegister(static s =>
            {
                var w = (WriteWaiter?)s;
                w?.TryCancel();
            }, waiter);
            if (cancellationToken.IsCancellationRequested)
            {
                waiter.CancellationRegistration.Dispose();
                waiter.TryCancel();
                return new(waiter.Tcs.Task);
            }
        }
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (waiter.Tcs.Task.IsCompleted)
            {
                return new(waiter.Tcs.Task);
            }

            // Double-check: re-attempt fast path under lock.
            if (Interlocked.CompareExchange(ref _state, WriterHeldBit, 0) == 0)
            {
                waiter.CancellationRegistration.Dispose();
                return new(new WriteRelease(this));
            }

            // Set WriterWaitingBit so the reader fast path starts blocking.
            // This is idempotent; multiple writers all set the same bit.
            SetWriterWaitingBit();
            waiter.Node = _writeWaiters.AddLast(waiter);
            _writeWaiterCount++;
        }
        return new(waiter.Tcs.Task);
    }

    // ── Internal release logic ────────────────────────────────────────────────

    private void ReleaseReadInternal()
    {
        // Atomically decrement the reader count.
        int current, next;
        do
        {
            current = Volatile.Read(ref _state);
            var count = current & ReaderCountMask;
            if (count == 0)
            {
                // Guard against double-release bugs.
                return;
            }
            next = current - 1;
        } while (Interlocked.CompareExchange(ref _state, next, current) != current);

        // If we were the last active reader and a writer is waiting, hand off to the writer.
        if ((next & ReaderCountMask) == 0 && (next & WriterWaitingBit) != 0)
        {
            HandOffToWriter();
        }
    }

    private void ReleaseWriteInternal()
    {
        // Release writer ownership; choose the next successor.
        while (true)
        {
            List<ReadWaiter>? readersToWake = null;
            WriteWaiter? nextWriter = null;
            lock (_sync)
            {
                if (_disposed)
                {
                    Volatile.Write(ref _state, 0);
                    return;
                }

                // Writer-preference: try pending writers first.
                while (_writeWaiters.Count > 0)
                {
                    nextWriter = _writeWaiters.First!.Value;
                    _writeWaiters.RemoveFirst();
                    nextWriter.Node = null;
                    _writeWaiterCount--;

                    // Recompute WriterWaitingBit based on remaining writers.
                    var newState = WriterHeldBit | (_writeWaiters.Count > 0 ? WriterWaitingBit : 0);
                    Volatile.Write(ref _state, newState);
                    break;
                }
                if (nextWriter is null)
                {
                    // No pending writers; batch-wake all pending readers.
                    if (_readWaiters.Count > 0)
                    {
                        readersToWake = new(_readWaiters.Count);
                        // Null out each waiter's Node before clearing so concurrent TryCancel
                        // callbacks see Node == null and skip the Remove() call safely.
                        while (_readWaiters.Count > 0)
                        {
                            var waiter = _readWaiters.First!.Value;
                            _readWaiters.RemoveFirst();
                            waiter.Node = null;
                            readersToWake.Add(waiter);
                        }
                        _readWaiterCount = 0;
                        // Pre-set reader count to how many we intend to wake;
                        // canceled ones will be corrected below.
                        Volatile.Write(ref _state, readersToWake.Count);
                    }
                    else
                    {
                        // Fully free.
                        Volatile.Write(ref _state, 0);
                        return;
                    }
                }
            }
            if (nextWriter is not null)
            {
                nextWriter.CancellationRegistration.Dispose();
                if (nextWriter.Tcs.TrySetResult(new(this)))
                {
                    return;
                }
                // Writer was canceled between dequeue and wake; loop to pick the next successor.
                continue;
            }

            // Wake collected readers. Each TrySetResult failure means the reader was already
            // canceled — subtract those from the state counter.
            if (readersToWake is null)
            {
                return;
            }
            var canceledCount = 0;
            foreach (var r in readersToWake)
            {
                r.CancellationRegistration.Dispose();
                if (!r.Tcs.TrySetResult(new(this)))
                {
                    canceledCount++;
                }
            }
            if (canceledCount > 0)
            {
                // Subtract readers that were already canceled (their slots were never actually used).
                Interlocked.Add(ref _state, -canceledCount);
            }
            return;
        }
    }

    /// <summary>
    /// Called when the last reader released and <c>WriterWaitingBit</c> is set.
    /// Hands ownership to the first non-canceled writer in the queue.
    /// </summary>
    private void HandOffToWriter()
    {
        while (true)
        {
            WriteWaiter? nextWriter;
            lock (_sync)
            {
                if (_disposed)
                {
                    Volatile.Write(ref _state, 0);
                    return;
                }
                if (_writeWaiters.Count == 0)
                {
                    // All writers canceled between last-reader-exit and now.
                    ClearWriterWaitingBit();
                    return;
                }
                nextWriter = _writeWaiters.First!.Value;
                _writeWaiters.RemoveFirst();
                nextWriter.Node = null;
                _writeWaiterCount--;

                // Transition: readers=0, WriterWaitingBit stays if more writers remain.
                Volatile.Write(ref _state, WriterHeldBit | (_writeWaiters.Count > 0 ? WriterWaitingBit : 0));
            }
            nextWriter.CancellationRegistration.Dispose();
            if (nextWriter.Tcs.TrySetResult(new(this)))
            {
                return;
            }
            // Selected writer was already canceled; try the next one.
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetWriterWaitingBit()
    {
        int current, next;
        do
        {
            current = Volatile.Read(ref _state);
            next = current | WriterWaitingBit;
        } while (Interlocked.CompareExchange(ref _state, next, current) != current);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ClearWriterWaitingBit()
    {
        int current, next;
        do
        {
            current = Volatile.Read(ref _state);
            next = current & ~WriterWaitingBit;
        } while (Interlocked.CompareExchange(ref _state, next, current) != current);
    }

    // ── Release value types ───────────────────────────────────────────────────

    /// <summary>
    ///     <para xml:lang="en">A disposable structure that releases the read lock when disposed.</para>
    ///     <para xml:lang="zh">一个可释放的结构，在释放时解除读锁。</para>
    /// </summary>
    public readonly struct ReadRelease : IDisposable
    {
        private readonly AsyncReaderWriterLock? _owner;

        internal ReadRelease(AsyncReaderWriterLock? owner)
        {
            _owner = owner;
        }

        /// <summary>
        ///     <para xml:lang="en">Releases the read lock.</para>
        ///     <para xml:lang="zh">释放读锁。</para>
        /// </summary>
        public void Dispose() => _owner?.ReleaseReadInternal();
    }

    /// <summary>
    ///     <para xml:lang="en">A disposable structure that releases the write lock when disposed.</para>
    ///     <para xml:lang="zh">一个可释放的结构，在释放时解除写锁。</para>
    /// </summary>
    public readonly struct WriteRelease : IDisposable
    {
        private readonly AsyncReaderWriterLock? _owner;

        internal WriteRelease(AsyncReaderWriterLock? owner)
        {
            _owner = owner;
        }

        /// <summary>
        ///     <para xml:lang="en">Releases the write lock.</para>
        ///     <para xml:lang="zh">释放写锁。</para>
        /// </summary>
        public void Dispose() => _owner?.ReleaseWriteInternal();
    }

    // ── Waiter types ──────────────────────────────────────────────────────────

    private sealed class ReadWaiter
    {
        private readonly AsyncReaderWriterLock _owner;

        // Completes with a ReadRelease when the lock is granted.
        internal readonly TaskCompletionSource<ReadRelease> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal CancellationTokenRegistration CancellationRegistration;
        internal CancellationToken CancellationToken;
        internal LinkedListNode<ReadWaiter>? Node;

        internal ReadWaiter(AsyncReaderWriterLock owner)
        {
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryCancel()
        {
            lock (_owner._sync)
            {
                // Only remove from queue if still enqueued; Node is set to null by both
                // ReleaseWriteInternal (batch wake) and Dispose() before they complete TCS,
                // so checking Node prevents a double-remove race.
                if (Node is not null)
                {
                    _owner._readWaiters.Remove(Node);
                    Node = null;
                    _owner._readWaiterCount--;
                }
            }
            CancellationRegistration.Dispose();
            // Always complete TCS so callers never hang — if the lock was already granted
            // (TrySetResult already called), this is a harmless no-op.
            // Queued readers were never counted in _state, so no state adjustment needed.
            Tcs.TrySetCanceled(CancellationToken);
        }
    }

    private sealed class WriteWaiter
    {
        private readonly AsyncReaderWriterLock _owner;

        // Completes with a WriteRelease when the lock is granted.
        internal readonly TaskCompletionSource<WriteRelease> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        internal CancellationTokenRegistration CancellationRegistration;
        internal CancellationToken CancellationToken;
        internal LinkedListNode<WriteWaiter>? Node;

        internal WriteWaiter(AsyncReaderWriterLock owner)
        {
            _owner = owner;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void TryCancel()
        {
            lock (_owner._sync)
            {
                // Only remove from queue if still enqueued; Node is set to null by
                // ReleaseWriteInternal/HandOffToWriter/Dispose() before they complete TCS.
                if (Node is not null)
                {
                    _owner._writeWaiters.Remove(Node);
                    Node = null;
                    _owner._writeWaiterCount--;

                    // If we were the last writer waiter, clear WriterWaitingBit atomically
                    // under the same lock to prevent a race where a new writer enqueues and
                    // sets the bit between our unlock and a deferred ClearWriterWaitingBit().
                    if (_owner._writeWaiters.Count == 0)
                    {
                        _owner.ClearWriterWaitingBit();
                    }
                }
            }
            CancellationRegistration.Dispose();
            // Always complete TCS so callers never hang — if the lock was already granted
            // (TrySetResult already called), this is a harmless no-op.
            Tcs.TrySetCanceled(CancellationToken);
        }
    }
}