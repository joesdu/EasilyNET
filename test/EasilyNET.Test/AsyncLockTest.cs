using EasilyNET.Core.Threading;
using Xunit;
using Xunit.Abstractions;

namespace EasilyNET.Test;

/// <summary>
/// 测试异步锁
/// </summary>
/// <remarks>
/// </remarks>
/// <param name="testOutputHelper"></param>
public class AsyncLockTests(ITestOutputHelper testOutputHelper)
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<string, string> _dictionary = [];

    /// <summary>
    /// 测试异步锁
    /// </summary>
    [Fact]
    public async Task TestAsyncLock()
    {
        var asyncLock = new AsyncLock();
        Parallel.For(0, 1000, Body);
        var c = _dictionary.Count;
        testOutputHelper.WriteLine($"Counter incremented to {c}");
        await Task.Delay(1);
        var c2 = _dictionary.Count;
        testOutputHelper.WriteLine($"Counter2 incremented to {c2}");
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
    [Fact]
    public async Task MultipleTasksAcquire()
    {
        var asyncLock = new AsyncLock();
        var task1 = asyncLock.LockAsync();
        var task2 = asyncLock.LockAsync();
        var task3 = asyncLock.LockAsync();
        await Task.Delay(5);
        Assert.False(task2.IsCompleted);
        Assert.False(task3.IsCompleted);

        //Released
        using (await task1)
        {
            // Lock acquired by task1
            Assert.False(task2.IsCompleted);
            Assert.False(task3.IsCompleted);
        }
        using (await task2)
        {
            Assert.True(task1.IsCompleted);
            Assert.False(task3.IsCompleted);
        }
        using (await task3)
        {
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
        }
    }

    /// <summary>
    /// 测试多线程
    /// </summary>
    [Fact]
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
        Assert.NotEqual(100, tasks.Count(x => x.IsCompleted));
        await Task.WhenAll(tasks);
        var count = tasks.Count(x => x.IsCompleted);
        Assert.Equal(100, count);
    }

    /// <summary>
    /// 测试Released
    /// </summary>
    [Fact]
    public async Task WaitingTasksAreReleasedWhenSemaphoreIsReleased()
    {
        var asyncLock = new AsyncLock();
        var taskSemaphore1 = asyncLock.LockAsync();
        var taskSemaphore2 = asyncLock.LockAsync();
        Assert.Equal(1, asyncLock.GetSemaphoreTaken());
        Assert.True(taskSemaphore1.IsCompleted);
        Assert.False(taskSemaphore2.IsCompleted);
        Assert.Equal(1, asyncLock.GetQueueCount());
        var res1 = await taskSemaphore1;
        res1.Dispose(); //释放
        Assert.True(taskSemaphore1.IsCompleted);
        var res2 = await taskSemaphore2;
        res2.Dispose(); //释放
        Assert.Equal(0, asyncLock.GetSemaphoreTaken());
    }
}