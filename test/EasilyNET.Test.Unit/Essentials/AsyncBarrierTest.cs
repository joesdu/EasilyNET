using EasilyNET.Core.Threading;

// ReSharper disable MethodSupportsCancellation

namespace EasilyNET.Test.Unit.Essentials;

[TestClass]
public class AsyncBarrierTest
{
    [TestMethod]
    public async Task AllParticipantsReachBarrier_ShouldContinueExecution()
    {
        var barrier = new AsyncBarrier(3);
        var cts = new CancellationTokenSource();
        var tasks = new List<Task>
        {
            Task.Run(async () => await barrier.SignalAndWait(cts.Token)),
            Task.Run(async () => await barrier.SignalAndWait(cts.Token)),
            Task.Run(async () => await barrier.SignalAndWait(cts.Token))
        };
        await Task.WhenAll(tasks);
    }

    [TestMethod]
    public async Task CancellationToken_ShouldCancelTask()
    {
        var barrier = new AsyncBarrier(3);
        var cts = new CancellationTokenSource();
        var tasks = new List<Task>
        {
            Task.Run(async () => await barrier.SignalAndWait(cts.Token)),
            Task.Run(async () => await barrier.SignalAndWait(cts.Token)),
            Task.Run(async () =>
            {
                await cts.CancelAsync(); // 取消令牌
                await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await barrier.SignalAndWait(cts.Token));
            })
        };
        try
        {
            await Task.WhenAll(tasks);
        }
        catch (TaskCanceledException)
        {
            // 预期的异常，不需要处理
        }
    }

    [TestMethod]
    public async Task CanceledWaiter_ShouldFreeSlot_AndNotDeadlockSubsequentBarrier()
    {
        var barrier = new AsyncBarrier(2);
        using var cts = new CancellationTokenSource();
        // First participant arrives and waits (barrier needs 2).
        var canceledWait = barrier.SignalAndWait(cts.Token);
        // Cancel it; the canceled waiter must be removed so it no longer occupies a participant slot.
        await cts.CancelAsync();
        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () => await canceledWait);
        // Reuse the barrier: two fresh participants must complete. If the canceled waiter still held a slot,
        // the first would pair with the dead waiter and the second would hang forever.
        var t1 = barrier.SignalAndWait(CancellationToken.None);
        var t2 = barrier.SignalAndWait(CancellationToken.None);
        await Task.WhenAll(t1.AsTask(), t2.AsTask()).WaitAsync(TimeSpan.FromSeconds(5));
    }

    [TestMethod]
    public async Task LessParticipants_ShouldWaitIndefinitely()
    {
        var barrier = new AsyncBarrier(3);
        var cts = new CancellationTokenSource();
        var tasks = new List<Task>
        {
            Task.Run(async () => await barrier.SignalAndWait(cts.Token)),
            Task.Run(async () => await barrier.SignalAndWait(cts.Token))
        };
        var delayTask = Task.Delay(1000, cts.Token);
        var allTasks = Task.WhenAll(tasks);
        var completedTask = await Task.WhenAny(allTasks, delayTask);
        Assert.AreEqual(delayTask, completedTask); // 确保任务在等待
    }
}