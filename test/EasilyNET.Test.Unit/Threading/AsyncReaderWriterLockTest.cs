using System.Diagnostics;
using EasilyNET.Core.Threading;

namespace EasilyNET.Test.Unit.Threading;

#pragma warning disable MSTEST0049

[TestClass]
public class AsyncReaderWriterLockTests
{
    /// <summary>
    /// 带超时保护的 await，防止回归导致测试进程永久挂起。
    /// </summary>
    private static async Task<T> AwaitWithTimeout<T>(Task<T> task, int timeoutMs = 5000, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(task))] string? expr = null)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeoutMs)))
        {
            return await task;
        }
        Assert.Fail($"Timed out after {timeoutMs}ms waiting for: {expr}");
        return default!; // unreachable
    }

    private static async Task AwaitWithTimeout(Task task, int timeoutMs = 5000, [System.Runtime.CompilerServices.CallerArgumentExpression(nameof(task))] string? expr = null)
    {
        if (task == await Task.WhenAny(task, Task.Delay(timeoutMs)))
        {
            await task; // propagate exceptions
            return;
        }
        Assert.Fail($"Timed out after {timeoutMs}ms waiting for: {expr}");
    }
    // ── Basic read-lock ────────────────────────────────────────────────────────

    /// <summary>
    /// 读锁获取与释放后 IsHeld 状态正确更新。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_AcquireAndRelease_ShouldUpdateIsHeld()
    {
        using var rwLock = new AsyncReaderWriterLock();
        Assert.IsFalse(rwLock.IsHeld);
        using (await rwLock.ReadLockAsync())
        {
            Assert.IsTrue(rwLock.IsHeld);
            Assert.IsFalse(rwLock.IsWriteHeld);
            Assert.AreEqual(1, rwLock.ReaderCount);
        }
        Assert.IsFalse(rwLock.IsHeld);
        Assert.AreEqual(0, rwLock.ReaderCount);
    }

    /// <summary>
    /// 多个读者可以同时持有读锁（共享访问）。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_MultipleReaders_CanHoldConcurrently()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var r1 = await rwLock.ReadLockAsync();
        var r2 = await rwLock.ReadLockAsync();
        var r3 = await rwLock.ReadLockAsync();
        Assert.AreEqual(3, rwLock.ReaderCount);
        r1.Dispose();
        Assert.AreEqual(2, rwLock.ReaderCount);
        r2.Dispose();
        Assert.AreEqual(1, rwLock.ReaderCount);
        r3.Dispose();
        Assert.AreEqual(0, rwLock.ReaderCount);
        Assert.IsFalse(rwLock.IsHeld);
    }

    // ── Basic write-lock ───────────────────────────────────────────────────────

    /// <summary>
    /// 写锁获取与释放后 IsWriteHeld / IsHeld 状态正确。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_AcquireAndRelease_ShouldUpdateIsWriteHeld()
    {
        using var rwLock = new AsyncReaderWriterLock();
        Assert.IsFalse(rwLock.IsWriteHeld);
        using (await rwLock.WriteLockAsync())
        {
            Assert.IsTrue(rwLock.IsWriteHeld);
            Assert.IsTrue(rwLock.IsHeld);
        }
        Assert.IsFalse(rwLock.IsWriteHeld);
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// 写锁持有时，第二个写锁请求必须等待（互斥）。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_MutualExclusion_SecondWriterMustWait()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var holder = await rwLock.WriteLockAsync();
        var secondTask = rwLock.WriteLockAsync().AsTask();

        // Second writer should NOT complete while first is still held.
        var completed = await Task.WhenAny(secondTask, Task.Delay(200));
        Assert.AreNotEqual(secondTask, completed, "Second writer should not have acquired the lock yet.");
        Assert.IsFalse(secondTask.IsCompleted);
        holder.Dispose();
        await AwaitWithTimeout(secondTask); // Should now complete
        secondTask.Result.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// 读锁持有时，写锁请求必须等待（互斥）。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_WhileReaderHolds_WriterMustWait()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var reader = await rwLock.ReadLockAsync();
        var writerTask = rwLock.WriteLockAsync().AsTask();
        var completed = await Task.WhenAny(writerTask, Task.Delay(200));
        Assert.AreNotEqual(writerTask, completed, "Writer should not have acquired the lock while reader holds it.");
        reader.Dispose();
        await AwaitWithTimeout(writerTask);
        writerTask.Result.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// 写锁持有时，新的读锁请求必须等待。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_WhileWriterHolds_ReaderMustWait()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var writer = await rwLock.WriteLockAsync();
        var readerTask = rwLock.ReadLockAsync().AsTask();
        var completed = await Task.WhenAny(readerTask, Task.Delay(200));
        Assert.AreNotEqual(readerTask, completed, "Reader should not have acquired the lock while writer holds it.");
        writer.Dispose();
        await AwaitWithTimeout(readerTask);
        readerTask.Result.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    // ── Writer-preference ──────────────────────────────────────────────────────

    /// <summary>
    /// 写者优先：写者入队后，新读者不能绕过写者先获取锁。
    /// </summary>
    [TestMethod]
    public async Task WriterPreference_NewReadersBlockedWhileWriterWaiting()
    {
        using var rwLock = new AsyncReaderWriterLock();

        // Start with a reader holding the lock.
        var firstReader = await rwLock.ReadLockAsync();

        // A writer waits — this should set WriterWaitingBit.
        var writerTask = rwLock.WriteLockAsync().AsTask();

        // Give the writer time to enqueue.
        var sw = Stopwatch.StartNew();
        while (rwLock.WriteWaiterCount != 1 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }
        Assert.AreEqual(1, rwLock.WriteWaiterCount, "Writer should be enqueued.");

        // A new reader arrives after the writer — it must be blocked (writer-preference).
        var lateReaderTask = rwLock.ReadLockAsync().AsTask();
        var completed = await Task.WhenAny(lateReaderTask, Task.Delay(200));
        Assert.AreNotEqual(lateReaderTask, completed, "Late reader must be blocked while writer is waiting.");

        // Release the initial reader — writer should be handed the lock.
        firstReader.Dispose();
        await AwaitWithTimeout(writerTask);
        Assert.IsTrue(rwLock.IsWriteHeld);

        // Release the writer — now the late reader can proceed.
        writerTask.Result.Dispose();
        await AwaitWithTimeout(lateReaderTask);
        lateReaderTask.Result.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    // ── Concurrent read serializes writes ─────────────────────────────────────

    /// <summary>
    /// 多个读者并发读，写者独占写，确保写操作被串行化。
    /// </summary>
    [TestMethod]
    public async Task ConcurrentReads_WithExclusiveWrite_ShouldSerializeWrites()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var shared = 0;
        var readCount = 0;

        // 5 readers and 3 writers racing.
        var readers = Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            using (await rwLock.ReadLockAsync())
            {
                Interlocked.Increment(ref readCount);
                await Task.Delay(20);
                Interlocked.Decrement(ref readCount);
            }
        })).ToArray();
        var writers = Enumerable.Range(0, 3).Select(_ => Task.Run(async () =>
        {
            using (await rwLock.WriteLockAsync())
            {
                var v = shared;
                await Task.Delay(10);
                shared = v + 1;
            }
        })).ToArray();
        await AwaitWithTimeout(Task.WhenAll(readers.Concat(writers)), 10000);
        Assert.AreEqual(3, shared, "Each writer should have incremented shared exactly once.");
    }

    // ── Cancellation ──────────────────────────────────────────────────────────

    /// <summary>
    /// 预取消的 Token 立即取消，不会获取读锁。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_PreCanceledToken_ThrowsWithoutAcquiring()
    {
        using var rwLock = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => rwLock.ReadLockAsync(cts.Token).AsTask());
        Assert.IsFalse(rwLock.IsHeld);
        Assert.AreEqual(0, rwLock.ReadWaiterCount);
    }

    /// <summary>
    /// 预取消的 Token 立即取消，不会获取写锁。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_PreCanceledToken_ThrowsWithoutAcquiring()
    {
        using var rwLock = new AsyncReaderWriterLock();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => rwLock.WriteLockAsync(cts.Token).AsTask());
        Assert.IsFalse(rwLock.IsHeld);
        Assert.AreEqual(0, rwLock.WriteWaiterCount);
    }

    /// <summary>
    /// 读者排队时被取消，取消后等待计数归零，后续读者能正常获取锁。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_CancelWhileQueued_RemovesWaiterAndAllowsNext()
    {
        using var rwLock = new AsyncReaderWriterLock();
        // Hold write lock so new readers must queue.
        var writer = await rwLock.WriteLockAsync();
        using var cts = new CancellationTokenSource();
        var queuedReader = rwLock.ReadLockAsync(cts.Token).AsTask();
        var sw = Stopwatch.StartNew();
        while (rwLock.ReadWaiterCount != 1 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }
        Assert.AreEqual(1, rwLock.ReadWaiterCount, "Reader should be enqueued.");
        await cts.CancelAsync();
        var ex = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await queuedReader);
        Assert.AreEqual(cts.Token, ex.CancellationToken);
        Assert.AreEqual(0, rwLock.ReadWaiterCount);

        // Release writer; a fresh reader should acquire normally.
        writer.Dispose();
        using (await rwLock.ReadLockAsync())
        {
            Assert.IsTrue(rwLock.IsHeld);
        }
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// 写者排队时被取消，取消后等待计数归零，后续写者能正常获取锁。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_CancelWhileQueued_RemovesWaiterAndAllowsNext()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var holder = await rwLock.WriteLockAsync();
        using var cts = new CancellationTokenSource();
        var queuedWriter = rwLock.WriteLockAsync(cts.Token).AsTask();
        var sw = Stopwatch.StartNew();
        while (rwLock.WriteWaiterCount != 1 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }
        Assert.AreEqual(1, rwLock.WriteWaiterCount, "Writer should be enqueued.");
        await cts.CancelAsync();
        var ex = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await queuedWriter);
        Assert.AreEqual(cts.Token, ex.CancellationToken);
        Assert.AreEqual(0, rwLock.WriteWaiterCount);

        // Release holder; a fresh writer should acquire normally.
        holder.Dispose();
        using (await rwLock.WriteLockAsync())
        {
            Assert.IsTrue(rwLock.IsWriteHeld);
        }
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// 最后一个排队写者被取消时，WriterWaitingBit 被清除，新读者可再次走快路径。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_LastWriterCanceled_ClearsWriterWaitingBit()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var reader = await rwLock.ReadLockAsync();
        using var cts = new CancellationTokenSource();
        var writerTask = rwLock.WriteLockAsync(cts.Token).AsTask();
        var sw = Stopwatch.StartNew();
        while (rwLock.WriteWaiterCount != 1 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }
        await cts.CancelAsync();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await writerTask);
        Assert.AreEqual(0, rwLock.WriteWaiterCount);

        // Now with no writer waiting, new readers should acquire immediately.
        reader.Dispose();
        using var r2 = await rwLock.ReadLockAsync();
        Assert.IsTrue(rwLock.IsHeld);
        Assert.IsFalse(rwLock.IsWriteHeld);
    }

    // ── Timeout overloads ──────────────────────────────────────────────────────

    /// <summary>
    /// ReadLockAsync 超时时返回 (false, default)，不抛异常。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_Timeout_ReturnsFalseOnExpiry()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var writer = await rwLock.WriteLockAsync();
        var (acquired, releaser) = await rwLock.ReadLockAsync(TimeSpan.FromMilliseconds(100));
        Assert.IsFalse(acquired);
        releaser.Dispose(); // should be a no-op (default)
        writer.Dispose();
    }

    /// <summary>
    /// WriteLockAsync 超时时返回 (false, default)，不抛异常。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_Timeout_ReturnsFalseOnExpiry()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var holder = await rwLock.WriteLockAsync();
        var (acquired, releaser) = await rwLock.WriteLockAsync(TimeSpan.FromMilliseconds(100));
        Assert.IsFalse(acquired);
        releaser.Dispose(); // should be a no-op (default)
        holder.Dispose();
    }

    /// <summary>
    /// ReadLockAsync 在超时前成功获取时返回 (true, releaser)。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_Timeout_ReturnsTrueWhenAcquired()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var (acquired, releaser) = await rwLock.ReadLockAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(acquired);
        Assert.IsTrue(rwLock.IsHeld);
        releaser.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// WriteLockAsync 在超时前成功获取时返回 (true, releaser)。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_Timeout_ReturnsTrueWhenAcquired()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var (acquired, releaser) = await rwLock.WriteLockAsync(TimeSpan.FromSeconds(1));
        Assert.IsTrue(acquired);
        Assert.IsTrue(rwLock.IsWriteHeld);
        releaser.Dispose();
        Assert.IsFalse(rwLock.IsHeld);
    }

    // ── Action/Func overloads ─────────────────────────────────────────────────

    /// <summary>
    /// ReadLockAsync(Func&lt;Task&gt;) 在锁内执行操作后正确释放。
    /// </summary>
    [TestMethod]
    public async Task ReadLockAsync_WithAction_ExecutesUnderLockAndReleases()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var executed = false;
        var heldDuring = false;
        await rwLock.ReadLockAsync(async () =>
        {
            heldDuring = rwLock.IsHeld;
            await Task.Delay(5);
            executed = true;
        });
        Assert.IsTrue(executed);
        Assert.IsTrue(heldDuring);
        Assert.IsFalse(rwLock.IsHeld);
    }

    /// <summary>
    /// WriteLockAsync(Func&lt;Task&lt;TResult&gt;&gt;) 返回正确结果并释放写锁。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_WithFunc_ReturnsResultAndReleasesLock()
    {
        using var rwLock = new AsyncReaderWriterLock();
        var result = await rwLock.WriteLockAsync(async () =>
        {
            await Task.Delay(5);
            return 42;
        });
        Assert.AreEqual(42, result);
        Assert.IsFalse(rwLock.IsHeld);
    }

    // ── TryReadLock / TryWriteLock ─────────────────────────────────────────────

    /// <summary>
    /// TryReadLock 在锁空闲时成功，持有时失败。
    /// </summary>
    [TestMethod]
    public async Task TryReadLock_SucceedsWhenFree_FailsWhenWriterHolds()
    {
        using var rwLock = new AsyncReaderWriterLock();
        Assert.IsTrue(rwLock.TryReadLock(out var r1));
        r1.Dispose();
        var writer = await rwLock.WriteLockAsync();
        Assert.IsFalse(rwLock.TryReadLock(out var r2));
        r2.Dispose(); // no-op
        writer.Dispose();
    }

    /// <summary>
    /// TryWriteLock 在锁空闲时成功，持有时失败。
    /// </summary>
    [TestMethod]
    public async Task TryWriteLock_SucceedsWhenFree_FailsWhenHeld()
    {
        using var rwLock = new AsyncReaderWriterLock();
        Assert.IsTrue(rwLock.TryWriteLock(out var w1));
        Assert.IsFalse(rwLock.TryWriteLock(out var w2));
        w2.Dispose(); // no-op
        w1.Dispose();
        Assert.IsFalse(rwLock.IsHeld);

        // After release, should succeed again.
        var reader = await rwLock.ReadLockAsync();
        Assert.IsFalse(rwLock.TryWriteLock(out var w3));
        w3.Dispose();
        reader.Dispose();
    }

    // ── Dispose behaviour ─────────────────────────────────────────────────────

    /// <summary>
    /// Dispose 后使用 AsyncReaderWriterLock 应抛出 ObjectDisposedException。
    /// </summary>
    [TestMethod]
    public async Task Dispose_UsageAfterDispose_ThrowsObjectDisposedException()
    {
        var rwLock = new AsyncReaderWriterLock();
        rwLock.Dispose();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => rwLock.ReadLockAsync().AsTask());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => rwLock.WriteLockAsync().AsTask());
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = rwLock.IsHeld; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = rwLock.IsWriteHeld; });
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = rwLock.ReaderCount; });
    }

    /// <summary>
    /// Dispose 时排队中的等待者应收到 ObjectDisposedException。
    /// </summary>
    [TestMethod]
    public async Task Dispose_WithQueuedWaiters_CompletesWaitersWithException()
    {
        var rwLock = new AsyncReaderWriterLock();
        // Hold the write lock so both queued waiters are blocked.
        _ = await rwLock.WriteLockAsync();

        // Queue one reader and one writer while the lock is held (do NOT dispose the holder).
        var queuedReader = rwLock.ReadLockAsync().AsTask();
        var queuedWriter = rwLock.WriteLockAsync().AsTask();

        // Wait until both are in the queue.
        var sw = Stopwatch.StartNew();
        while (rwLock.ReadWaiterCount + rwLock.WriteWaiterCount < 2 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }

        // Dispose while the lock is still held — queued waiters should receive ObjectDisposedException.
        rwLock.Dispose();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await queuedReader);
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () => await queuedWriter);
    }

    // ── Non-reentrant ─────────────────────────────────────────────────────────

    /// <summary>
    /// AsyncReaderWriterLock 不可重入：同一流中重复申请写锁会死锁。
    /// </summary>
    [TestMethod]
    public async Task WriteLockAsync_NonReentrant_DeadlocksOnReentry()
    {
        using var rwLock = new AsyncReaderWriterLock();
        using var holder = await rwLock.WriteLockAsync();
        var reentrantTask = rwLock.WriteLockAsync().AsTask();
        var completed = await Task.WhenAny(reentrantTask, Task.Delay(200));
        Assert.AreNotEqual(reentrantTask, completed, "Reentrant write lock should deadlock.");
        Assert.IsFalse(reentrantTask.IsCompleted);
    }
}