using EasilyNET.Core.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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