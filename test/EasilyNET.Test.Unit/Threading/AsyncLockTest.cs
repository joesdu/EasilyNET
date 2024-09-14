using EasilyNET.Core.Threading;
using FluentAssertions;

namespace EasilyNET.Test.Unit.Threading;

/// <summary>
/// 测试异步锁
/// </summary>
[TestClass]
public class AsyncLockTest(TestContext testContext)
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<string, string> _dictionary = [];

    /// <summary>
    /// 测试异步锁
    /// </summary>
    [TestMethod]
    public async Task TestAsyncLock()
    {
        var asyncLock = new AsyncLock();
        Parallel.For(0, 1000, Body);
        var c = _dictionary.Count;
        testContext.WriteLine($"Counter incremented to {c}");
        await Task.Delay(1);
        var c2 = _dictionary.Count;
        testContext.WriteLine($"Counter2 incremented to {c2}");
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
        task2.IsCompleted.Should().BeFalse();
        task3.IsCompleted.Should().BeFalse();

        //Released
        using (await task1)
        {
            // Lock acquired by task1
            task2.IsCompleted.Should().BeFalse();
            task3.IsCompleted.Should().BeFalse();
        }
        using (await task2)
        {
            task1.IsCompleted.Should().BeTrue();
            task3.IsCompleted.Should().BeFalse();
        }
        using (await task3)
        {
            task1.IsCompleted.Should().BeTrue();
            task2.IsCompleted.Should().BeTrue();
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
        tasks.Count(x => x.IsCompleted).Should().NotBe(100);
        await Task.WhenAll(tasks);
        var count = tasks.Count(x => x.IsCompleted);
        count.Should().Be(100);
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
        asyncLock.GetSemaphoreTaken().Should().Be(1);
        taskSemaphore1.IsCompleted.Should().BeTrue();
        taskSemaphore2.IsCompleted.Should().BeFalse();
        asyncLock.GetQueueCount().Should().Be(1);
        var res1 = await taskSemaphore1;
        res1.Dispose(); //释放
        taskSemaphore1.IsCompleted.Should().BeTrue();
        var res2 = await taskSemaphore2;
        res2.Dispose(); //释放
        asyncLock.GetSemaphoreTaken().Should().Be(0);
    }
}