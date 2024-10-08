using EasilyNET.Core.Threading;

namespace EasilyNET.Test.Unit.Threading;

[TestClass]
public class SyncLockTest
{
    /// <summary>
    /// 测试基本锁定和释放功能，确保 _isTaken 状态正确。
    /// </summary>
    [TestMethod]
    public void Lock_ShouldLockAndRelease()
    {
        var syncLock = new SyncLock();
        Assert.AreEqual(0, syncLock.GetSemaphoreTaken());
        using (syncLock.Lock())
        {
            Assert.AreEqual(1, syncLock.GetSemaphoreTaken());
        }
        Assert.AreEqual(0, syncLock.GetSemaphoreTaken());
    }

    /// <summary>
    /// 测试 Lock(Action action) 方法，确保传入的操作被执行。
    /// </summary>
    [TestMethod]
    public void Lock_ShouldExecuteAction()
    {
        var syncLock = new SyncLock();
        var executed = false;
        syncLock.Lock(() => executed = true);
        Assert.IsTrue(executed);
    }

    /// <summary>
    /// 测试在锁定时，后续任务会排队等待，确保任务按顺序执行。
    /// </summary>
    [TestMethod]
    public void Lock_ShouldQueueWhenLocked()
    {
        var syncLock = new SyncLock();
        var task1Completed = false;
        var task2Completed = false;
        var task1 = Task.Run(() =>
        {
            using (syncLock.Lock())
            {
                Thread.Sleep(100);
                task1Completed = true;
            }
        });
        var task2 = Task.Run(() =>
        {
            using (syncLock.Lock())
            {
                task2Completed = true;
            }
        });
        Task.WaitAll(task1, task2);
        Assert.IsTrue(task1Completed);
        Assert.IsTrue(task2Completed);
    }

    /// <summary>
    /// 测试多线程环境下的锁定行为，确保计数器正确增加。
    /// </summary>
    [TestMethod]
    public void Lock_ShouldHandleMultipleThreads()
    {
        var syncLock = new SyncLock();
        var counter = 0;
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (var j = 0; j < 100; j++)
                {
                    syncLock.Lock(() => counter++);
                }
            });
        }
        Task.WaitAll(tasks);
        Assert.AreEqual(1000, counter);
    }

    /// <summary>
    /// 测试锁的重入行为，确保锁不允许重入。
    /// </summary>
    [TestMethod]
    public void Lock_ShouldNotAllowReentrancy()
    {
        var syncLock = new SyncLock();
        var reentrancyDetected = false;
        try
        {
            syncLock.Lock(() => syncLock.Lock(() => true));
        }
        catch (InvalidOperationException)
        {
            reentrancyDetected = true;
        }
        Assert.IsTrue(reentrancyDetected);
    }
}