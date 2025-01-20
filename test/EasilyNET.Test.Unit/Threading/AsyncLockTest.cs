using EasilyNET.Core.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Shouldly;

namespace EasilyNET.Test.Unit.Threading;

[TestClass]
public class AsyncLockTests
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<string, string> _dictionary = [];

    public TestContext? TestContext { get; set; }

    /// <summary>
    /// 测试异步锁
    /// </summary>
    [TestMethod]
    public async Task TestAsyncLock()
    {
        var asyncLock = new AsyncLock();
        Parallel.For(0, 1000, Body);
        var c = _dictionary.Count;
        TestContext?.WriteLine($"Counter incremented to {c}");
        await Task.Delay(1);
        var c2 = _dictionary.Count;
        TestContext?.WriteLine($"Counter2 incremented to {c2}");
        return;

        async void Body(int i)
        {
            var k = i;
            //不会并发冲突
            using (await asyncLock.LockAsync())
            {
                await Task.Run(() => _dictionary.Add(k.ToString(), k.ToString()));
            }
        }
    }

    /// <summary>
    /// 获取锁
    /// </summary>
    [TestMethod]
    public async Task MultipleTasksAcquire()
    {
        var asyncLock = new AsyncLock();
        var task1 = asyncLock.LockAsync();
        var task2 = asyncLock.LockAsync();
        var task3 = asyncLock.LockAsync();
        await Task.Delay(5);
        task2.IsCompleted.ShouldBeFalse();
        task3.IsCompleted.ShouldBeFalse();

        //Released
        using (await task1)
        {
            // Lock acquired by task1
            task2.IsCompleted.ShouldBeFalse();
            task3.IsCompleted.ShouldBeFalse();
        }
        using (await task2)
        {
            task1.IsCompleted.ShouldBeTrue();
            task3.IsCompleted.ShouldBeFalse();
        }
        using (await task3)
        {
            task1.IsCompleted.ShouldBeTrue();
            task2.IsCompleted.ShouldBeTrue();
        }
    }

    /// <summary>
    /// 测试多线程
    /// </summary>
    [TestMethod]
    public async Task LockCanHandleMultipleConcurrentTasks()
    {
        var asyncLock = new AsyncLock();
        var tasks = new List<Task>();
        for (var i = 0; i < 100; i++)
        {
            tasks.Add(Task.Run(async () =>
            {
                using (await asyncLock.LockAsync())
                {
                    await Task.Delay(10);
                }
            }));
        }
        tasks.Count(x => x.IsCompleted).ShouldNotBe(100);
        await Task.WhenAll(tasks);
        var count = tasks.Count(x => x.IsCompleted);
        count.ShouldBe(100);
    }

    /// <summary>
    /// 测试Released
    /// </summary>
    [TestMethod]
    public async Task WaitingTasksAreReleasedWhenSemaphoreIsReleased()
    {
        var asyncLock = new AsyncLock();
        var taskSemaphore1 = asyncLock.LockAsync();
        var taskSemaphore2 = asyncLock.LockAsync();
        asyncLock.GetSemaphoreTaken().ShouldBe(1);
        taskSemaphore1.IsCompleted.ShouldBeTrue();
        taskSemaphore2.IsCompleted.ShouldBeFalse();
        asyncLock.GetQueueCount().ShouldBe(1);
        var res1 = await taskSemaphore1;
        res1.Dispose(); //释放
        taskSemaphore1.IsCompleted.ShouldBeTrue();
        var res2 = await taskSemaphore2;
        res2.Dispose(); //释放
        asyncLock.GetSemaphoreTaken().ShouldBe(0);
    }

    /// <summary>
    /// 测试基本锁定和释放功能，确保 _isTaken 状态正确。
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ShouldLockAndRelease()
    {
        var asyncLock = new AsyncLock();
        Assert.AreEqual(0, asyncLock.GetSemaphoreTaken());
        using (await asyncLock.LockAsync())
        {
            Assert.AreEqual(1, asyncLock.GetSemaphoreTaken());
        }
        Assert.AreEqual(0, asyncLock.GetSemaphoreTaken());
    }

    /// <summary>
    /// 测试 LockAsync(Action action) 方法，确保传入的操作被执行。
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ShouldExecuteAction()
    {
        var asyncLock = new AsyncLock();
        var executed = false;
        await asyncLock.LockAsync(() => Task.Run(() => executed = true));
        Assert.IsTrue(executed);
    }

    /// <summary>
    /// 测试在锁定时，后续任务会排队等待，确保任务按顺序执行。
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ShouldQueueWhenLocked()
    {
        var asyncLock = new AsyncLock();
        var task1Completed = false;
        var task2Completed = false;
        var task1 = Task.Run(async () =>
        {
            using (await asyncLock.LockAsync())
            {
                await Task.Delay(100);
                task1Completed = true;
            }
        });
        var task2 = Task.Run(async () =>
        {
            using (await asyncLock.LockAsync())
            {
                task2Completed = true;
            }
        });
        await Task.WhenAll(task1, task2);
        Assert.IsTrue(task1Completed);
        Assert.IsTrue(task2Completed);
    }

    /// <summary>
    /// 测试多线程环境下的锁定行为，确保计数器正确增加。
    /// </summary>
    [TestMethod]
    public async Task LockAsync_ShouldHandleMultipleThreads()
    {
        var asyncLock = new AsyncLock();
        var counter = 0;
        var tasks = new Task[10];
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                for (var j = 0; j < 100; j++)
                {
                    await asyncLock.LockAsync(async () => await Task.Run(() => counter++));
                }
            });
        }
        await Task.WhenAll(tasks);
        Assert.AreEqual(1000, counter);
    }

    ///// <summary>
    ///// 测试锁的重入行为，确保锁不允许重入。
    ///// </summary>
    //[TestMethod]
    ////[Ignore] // 异步锁线程都可以重入, 但是不建议这样做
    //public async Task Lock_ShouldNotAllowReentrancy()
    //{
    //    var syncLock = new AsyncLock();
    //    var reentrancyDetected = false;
    //    try
    //    {
    //        using (await syncLock.LockAsync())
    //        {
    //            using (await syncLock.LockAsync())
    //            {
    //                await Task.Delay(100);
    //            }
    //        }
    //    }
    //    catch (InvalidOperationException)
    //    {
    //        reentrancyDetected = true;
    //    }
    //    Assert.IsTrue(reentrancyDetected);
    //}
}