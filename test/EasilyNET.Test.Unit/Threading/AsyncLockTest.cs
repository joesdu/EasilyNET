using EasilyNET.Core.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace EasilyNET.Test.Unit.Threading;

[TestClass]
public class AsyncLockTests
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<string, string> _dictionary = []; // Made static for TestAsyncLock, ensure cleanup

    public TestContext? TestContext { get; set; }

    [TestInitialize]
    public void TestInitialize()
    {
        _dictionary.Clear(); // Clear the static dictionary before each test
    }

    /// <summary>
    /// Tests basic lock acquisition and release using the disposable Release struct.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_AcquireAndRelease_ShouldUpdateIsHeld()
    {
        using var asyncLock = new AsyncLock();
        asyncLock.IsHeld.ShouldBeFalse();
        using (await asyncLock.LockAsync())
        {
            asyncLock.IsHeld.ShouldBeTrue();
        }
        asyncLock.IsHeld.ShouldBeFalse();
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
                asyncLock.IsHeld.ShouldBeTrue();
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        var task2 = Task.Run(async () =>
        {
            await task1LockAcquired.Task;    // Ensure task1 has acquired the lock
            await Task.Delay(5);             // Give task1 time to be in the lock
            asyncLock.IsHeld.ShouldBeTrue(); // Lock should still be held by task1
            using (await asyncLock.LockAsync())
            {
                task2LockAcquired.SetResult(true);
                asyncLock.IsHeld.ShouldBeTrue();
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        var task3 = Task.Run(async () =>
        {
            await task2LockAcquired.Task;    // Ensure task2 has acquired the lock
            await Task.Delay(5);             // Give task2 time to be in the lock
            asyncLock.IsHeld.ShouldBeTrue(); // Lock should still be held by task2
            using (await asyncLock.LockAsync())
            {
                task3LockAcquired.SetResult(true);
                asyncLock.IsHeld.ShouldBeTrue();
                await Task.Delay(50); // Hold the lock for a bit
            }
        });
        await Task.WhenAll(task1, task2, task3);
        task1LockAcquired.Task.Result.ShouldBeTrue();
        task2LockAcquired.Task.Result.ShouldBeTrue();
        task3LockAcquired.Task.Result.ShouldBeTrue();
        asyncLock.IsHeld.ShouldBeFalse(); // All tasks completed, lock should be released
    }

    /// <summary>
    /// Tests concurrent access to a shared resource, ensuring the lock serializes access.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ConcurrentAccessToSharedResource_ShouldSerializeAccess()
    {
        var asyncLock = new AsyncLock();
        var tasks = new List<Task>();
        const int numTasks = 100;
        for (var i = 0; i < numTasks; i++)
        {
            var k = i;
            tasks.Add(Task.Run(async () =>
            {
                using (await asyncLock.LockAsync())
                {
                    // Simulate some work
                    await Task.Delay(1);
                    _dictionary.Add(k.ToString(), k.ToString());
                }
            }));
        }
        await Task.WhenAll(tasks);
        _dictionary.Count.ShouldBe(numTasks);
        TestContext?.WriteLine($"Dictionary count after concurrent adds: {_dictionary.Count}");
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
        executed.ShouldBeTrue();
        lockHeldDuringAction.ShouldBeTrue();
        asyncLock.IsHeld.ShouldBeFalse(); // Lock should be released after action
    }

    /// <summary>
    /// Tests the LockAsync&lt;TResult&gt;(Func&lt;Task&lt;TResult&gt;&gt; func) overload.
    /// </summary>
    [TestMethod]
    public async Task LockAsync_WithFunc_ShouldExecuteFuncUnderLockAndReturnResult()
    {
        using var asyncLock = new AsyncLock();
        var expectedResult = "test_result";
        var lockHeldDuringFunc = false;
        var result = await asyncLock.LockAsync(async () =>
        {
            lockHeldDuringFunc = asyncLock.IsHeld;
            await Task.Delay(10); // Simulate async work
            return expectedResult;
        });
        result.ShouldBe(expectedResult);
        lockHeldDuringFunc.ShouldBeTrue();
        asyncLock.IsHeld.ShouldBeFalse(); // Lock should be released after func
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
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await lockTask);
        releaser.Dispose(); // Release the initially acquired lock
        asyncLock.IsHeld.ShouldBeFalse();
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
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await lockActionTask);
        actionExecuted.ShouldBeFalse(); // Action should not have been executed
        releaser.Dispose();             // Release the initially acquired lock
        asyncLock.IsHeld.ShouldBeFalse();
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
        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () => await lockFuncTask);
        funcExecuted.ShouldBeFalse(); // Func should not have been executed
        releaser.Dispose();           // Release the initially acquired lock
        asyncLock.IsHeld.ShouldBeFalse();
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

        // IsHeld on a disposed AsyncLock will throw ObjectDisposedException because SemaphoreSlim.CurrentCount throws.
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
        completedTask.ShouldNotBe(reentrantTask, "Lock should have deadlocked due to non-reentrancy.");
        reentrantTask.IsCompleted.ShouldBeFalse();

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
        asyncLock.IsHeld.ShouldBeFalse();
        var releaser = await asyncLock.LockAsync();
        asyncLock.IsHeld.ShouldBeTrue();
        releaser.Dispose();
        asyncLock.IsHeld.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the WaitingCount property reflects tasks waiting for the lock.
    /// Note: SemaphoreSlim does not directly expose a waiter count.
    /// The current AsyncLock.WaitingCount is a simplification based on whether the lock is held.
    /// This test verifies its current behavior.
    /// </summary>
    [TestMethod]
    public async Task WaitingCount_ShouldReflectWaitingTasks()
    {
        using var asyncLock = new AsyncLock();
        asyncLock.WaitingCount.ShouldBe(0);
        var releaser1 = await asyncLock.LockAsync(); // Lock is now held
        asyncLock.IsHeld.ShouldBeTrue();
        // WaitingCount is 1 if held, 0 otherwise, according to current AsyncLock implementation.
        asyncLock.WaitingCount.ShouldBe(1);
        var waitingTask = asyncLock.LockAsync(); // This task will wait
        // At this point, one task holds the lock, another is (conceptually) waiting.
        // The current simplified WaitingCount will still be 1 because IsHeld is true.
        asyncLock.WaitingCount.ShouldBe(1); // Still 1 as it's based on IsHeld
        releaser1.Dispose();                // Release the first lock
        var releaser2 = await waitingTask;  // The waiting task should now acquire the lock
        asyncLock.IsHeld.ShouldBeTrue();    // Held by the task that was waiting
        asyncLock.WaitingCount.ShouldBe(1);
        releaser2.Dispose(); // Release the second lock
        asyncLock.IsHeld.ShouldBeFalse();
        asyncLock.WaitingCount.ShouldBe(0);
    }
}