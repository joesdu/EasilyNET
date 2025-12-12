using System.Diagnostics;
using EasilyNET.Core.Threading;

namespace EasilyNET.Test.Unit.Threading;

[TestClass]
public class AsyncLockTests
{
    // ReSharper disable once CollectionNeverQueried.Local
    // ReSharper disable once CollectionNeverUpdated.Local
    private static readonly Dictionary<string, string> Dic = []; // Made static for TestAsyncLock, ensure cleanup

    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        Dic.Clear(); // Clear the static dictionary before each test
    }

    /// <summary>
    /// Tests basic lock acquisition and release using the disposable Release struct.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_AcquireAndRelease_ShouldUpdateIsHeld()
    {
        using var asyncLock = new AsyncLock();
        Assert.IsFalse(asyncLock.IsHeld);
        using (await asyncLock.LockAsync())
        {
            Assert.IsTrue(asyncLock.IsHeld);
        }
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests that multiple tasks queue for the lock and acquire it sequentially.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_MultipleTasksAcquireSequentially()
    {
        var asyncLock = new AsyncLock();
        var task1LockAcquired = new TaskCompletionSource<bool>();
        var task2LockAcquired = new TaskCompletionSource<bool>();
        var task3LockAcquired = new TaskCompletionSource<bool>();
        var task1 = Task.Run(async () =>
        {
            using (await asyncLock.LockAsync())
            {
                task1LockAcquired.SetResult(true);
                Assert.IsTrue(asyncLock.IsHeld);
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        var task2 = Task.Run(async () =>
        {
            await task1LockAcquired.Task;    // Ensure task1 has acquired the lock
            await Task.Delay(5);             // Give task1 time to be in the lock
            Assert.IsTrue(asyncLock.IsHeld); // Lock should still be held by task1
            using (await asyncLock.LockAsync())
            {
                task2LockAcquired.SetResult(true);
                Assert.IsTrue(asyncLock.IsHeld);
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        var task3 = Task.Run(async () =>
        {
            await task2LockAcquired.Task;    // Ensure task2 has acquired the lock
            await Task.Delay(5);             // Give task2 time to be in the lock
            Assert.IsTrue(asyncLock.IsHeld); // Lock should still be held by task2
            using (await asyncLock.LockAsync())
            {
                task3LockAcquired.SetResult(true);
                Assert.IsTrue(asyncLock.IsHeld);
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        await Task.WhenAll(task1, task2, task3);
        Assert.IsTrue(task1LockAcquired.Task.Result);
        Assert.IsTrue(task2LockAcquired.Task.Result);
        Assert.IsTrue(task3LockAcquired.Task.Result);
        Assert.IsFalse(asyncLock.IsHeld); // All tasks completed, lock should be released
    }

    /// <summary>
    /// Tests concurrent access to a shared resource, ensuring the lock serializes access.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ConcurrentAccessToSharedResource_ShouldSerializeAccess()
    {
        var lockObj = new AsyncLock();
        var shared = 0;
        var tasks = Enumerable.Range(0, 10).Select(_ => AccessShared()).ToArray();
        await Task.WhenAll(tasks);
        Assert.AreEqual(10, shared); // Ensure increments are serialized
        TestContext?.WriteLine($"Dictionary count after concurrent adds: {Dic.Count}");
        return;

        async Task AccessShared()
        {
            using (await lockObj.LockAsync())
            {
                var initial = shared;
                await Task.Delay(50); // Simulate work
                shared = initial + 1;
            }
        }
    }

    /// <summary>
    /// Tests the LockAsync(Func&lt;Task&gt; action) overload.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WithAction_ShouldExecuteActionUnderLock()
    {
        using var asyncLock = new AsyncLock();
        var executed = false;
        var lockHeldDuringAction = false;
        await asyncLock.LockAsync(async () =>
        {
            lockHeldDuringAction = asyncLock.IsHeld;
            await Task.Delay(10); // Simulate async work
            executed = true;
        });
        Assert.IsTrue(executed);
        Assert.IsTrue(lockHeldDuringAction);
        Assert.IsFalse(asyncLock.IsHeld); // Lock should be released after action
    }

    /// <summary>
    /// Tests the LockAsync&lt;TResult&gt;(Func&lt;Task&lt;TResult&gt;&gt; func) overload.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WithFunc_ShouldExecuteFuncUnderLockAndReturnResult()
    {
        using var asyncLock = new AsyncLock();
        const string expectedResult = "test_result";
        var lockHeldDuringFunc = false;
        var result = await asyncLock.LockAsync(async () =>
        {
            lockHeldDuringFunc = asyncLock.IsHeld;
            await Task.Delay(10); // Simulate async work
            return expectedResult;
        });
        Assert.AreEqual(expectedResult, result);
        Assert.IsTrue(lockHeldDuringFunc);
        Assert.IsFalse(asyncLock.IsHeld); // Lock should be released after func
    }

    /// <summary>
    /// Tests cancellation of LockAsync() before the lock is acquired.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_CancellationToken_ShouldCancelIfNotAcquired()
    {
        using var asyncLock = new AsyncLock();
        using var cts = new CancellationTokenSource();

        // Acquire the lock so the next attempt has to wait
        var releaser = await asyncLock.LockAsync(cts.Token); // Pass token here as well
        var lockTask = asyncLock.LockAsync(cts.Token);
        await cts.CancelAsync(); // Use CancelAsync for async operations
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await lockTask);
        releaser.Dispose(); // Release the initially acquired lock
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests that a pre-canceled token causes immediate cancellation and does not acquire the lock.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_PreCanceledToken_ShouldThrowWithoutAcquiring()
    {
        using var asyncLock = new AsyncLock();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(() => asyncLock.LockAsync(cts.Token));
        Assert.IsFalse(asyncLock.IsHeld);
        Assert.AreEqual(0, asyncLock.WaitingCount);
    }

    /// <summary>
    /// Tests cancellation while queued removes the waiter and frees the lock for subsequent waiters.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_CancellationWhileQueued_ShouldRemoveWaiter()
    {
        using var asyncLock = new AsyncLock();
        using var holder = await asyncLock.LockAsync();
        using var cts = new CancellationTokenSource();
        var queuedTask = asyncLock.LockAsync(cts.Token);

        // Wait until the waiter is enqueued, or timeout after 1 second
        var sw = Stopwatch.StartNew();
        while (asyncLock.WaitingCount != 1 && sw.ElapsedMilliseconds < 1000)
        {
            await Task.Delay(5);
        }
        Assert.AreEqual(1, asyncLock.WaitingCount, "Waiter was not enqueued within timeout.");
        cts.Cancel();
        var ex = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await queuedTask);
        Assert.AreEqual(cts.Token, ex.CancellationToken);
        Assert.AreEqual(0, asyncLock.WaitingCount);
        var thirdAcquired = false;
        var thirdTask = Task.Run(async () =>
        {
            using (await asyncLock.LockAsync())
            {
                thirdAcquired = true;
            }
        });
        holder.Dispose();
        await thirdTask;
        Assert.IsTrue(thirdAcquired);
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests cancellation of LockAsync(Func&lt;Task&gt; action, CancellationToken) before the lock is acquired.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_Action_CancellationToken_ShouldCancelIfNotAcquired()
    {
        using var asyncLock = new AsyncLock();
        using var cts = new CancellationTokenSource();
        var actionExecuted = false;

        // Acquire the lock so the next attempt has to wait
        var releaser = await asyncLock.LockAsync(cts.Token); // Pass token here as well
        var lockActionTask = asyncLock.LockAsync(async () =>
        {
            actionExecuted = true;
            await Task.CompletedTask;
        }, cts.Token);
        await cts.CancelAsync(); // Use CancelAsync
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await lockActionTask);
        Assert.IsFalse(actionExecuted); // Action should not have been executed
        releaser.Dispose();             // Release the initially acquired lock
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests cancellation of LockAsync&lt;TResult&gt;(Func&lt;Task&lt;TResult&gt;&gt; func, CancellationToken) before the lock is acquired.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_Func_CancellationToken_ShouldCancelIfNotAcquired()
    {
        using var asyncLock = new AsyncLock();
        var cts = new CancellationTokenSource();
        var funcExecuted = false;

        // Acquire the lock so the next attempt has to wait
        var releaser = await asyncLock.LockAsync(cts.Token); // Pass token here as well
        var lockFuncTask = asyncLock.LockAsync(async () =>
        {
            funcExecuted = true;
            await Task.Delay(10, cts.Token); // Simulate work that also observes token
            return "done";
        }, cts.Token);
        await cts.CancelAsync(); // Use CancelAsync
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await lockFuncTask);
        Assert.IsFalse(funcExecuted); // Func should not have been executed
        releaser.Dispose();           // Release the initially acquired lock
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests that attempting to use a disposed AsyncLock throws ObjectDisposedException.
    /// </summary>
    [TestMethod]
    public async Task AsyncLock_UsageAfterDispose_ShouldThrowObjectDisposedException()
    {
        var asyncLock = new AsyncLock();
        asyncLock.Dispose();
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => asyncLock.LockAsync());
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => asyncLock.LockAsync(async () => await Task.CompletedTask));
        await Assert.ThrowsExactlyAsync<ObjectDisposedException>(() => asyncLock.LockAsync(async () => await Task.FromResult(true)));

        // IsHeld on a disposed AsyncLock throws ObjectDisposedException.
        Assert.ThrowsExactly<ObjectDisposedException>(() => { _ = asyncLock.IsHeld; });
    }

    /// <summary>
    /// Tests that the lock is not reentrant.
    /// Attempting to acquire the lock again from the same asynchronous flow while it is already held should deadlock.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ShouldNotBeReentrant_AndDeadlock()
    {
        using var asyncLock = new AsyncLock();
        using var releaser = await asyncLock.LockAsync(); // Acquire the lock and ensure it's released after the test
        var reentrantTask = asyncLock.LockAsync();        // Attempt to acquire again

        // The task should not complete because it's waiting for the lock that this flow already holds.
        var completedTask = await Task.WhenAny(reentrantTask, Task.Delay(TimeSpan.FromMilliseconds(200)));
        Assert.AreNotEqual(reentrantTask, completedTask, "Lock should have deadlocked due to non-reentrancy.");
        Assert.IsFalse(reentrantTask.IsCompleted);

        // No explicit cleanup for reentrantTask needed here as the test is verifying it doesn't complete.
        // The `using var releaser` will dispose the first lock, and `using var asyncLock` will dispose the AsyncLock itself.
        // If reentrantTask were to acquire a lock, it would need its own using or manual dispose.
    }

    /// <summary>
    /// Tests that disposing the Release struct releases the lock.
    /// </summary>
    [TestMethod]
    public async Task Release_Dispose_ShouldReleaseLock()
    {
        using var asyncLock = new AsyncLock();
        Assert.IsFalse(asyncLock.IsHeld);
        var releaser = await asyncLock.LockAsync();
        Assert.IsTrue(asyncLock.IsHeld);
        releaser.Dispose();
        Assert.IsFalse(asyncLock.IsHeld);
    }

    /// <summary>
    /// Tests that the WaitingCount property reflects the actual number of queued waiters.
    /// </summary>
    [TestMethod]
    public async Task WaitingCount_ShouldReflectWaitingTasks()
    {
        using var asyncLock = new AsyncLock();
        Assert.AreEqual(0, asyncLock.WaitingCount);

        // Acquire lock; no waiters yet.
        var releaser1 = await asyncLock.LockAsync();
        Assert.IsTrue(asyncLock.IsHeld);
        Assert.AreEqual(0, asyncLock.WaitingCount);

        // Start a waiter; it should now be queued.
        var waitingTask = asyncLock.LockAsync();
        Assert.AreEqual(1, asyncLock.WaitingCount);

        // Release first holder; waiting task should acquire and queue becomes empty.
        releaser1.Dispose();
        var releaser2 = await waitingTask;
        Assert.IsTrue(asyncLock.IsHeld);
        Assert.AreEqual(0, asyncLock.WaitingCount);

        // Release second holder; lock is free and no waiters.
        releaser2.Dispose();
        Assert.IsFalse(asyncLock.IsHeld);
        Assert.AreEqual(0, asyncLock.WaitingCount);
    }
}