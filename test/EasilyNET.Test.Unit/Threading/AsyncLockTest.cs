using EasilyNET.Core.Threading;

namespace EasilyNET.Test.Unit.Threading;

/// <summary>
/// 测试异步锁
/// </summary>
[TestClass]
public class AsyncLockTest
{
    // ReSharper disable once CollectionNeverQueried.Local
    private static readonly Dictionary<string, string> _dictionary = [];

    /// <summary>
    /// 测试异步锁
    /// </summary>
    [TestMethod]
    public Task TestAsyncLock()
    {
        var asyncLock = new AsyncLock();
        Parallel.For(0, 1000, Body);
        return Task.CompletedTask;

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
}