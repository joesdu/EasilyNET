using EasilyNET.MongoDistributedLock.Attributes;
using EasilyNET.WebCore.Swagger.Attributes;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// MongoDB分布式锁测试
/// </summary>
[Route("api/[controller]/[action]"), ApiController, ApiGroup("Distributed", "v1", "分布式锁测试")]
public class DistributedLockController(IDistributedLock mongoLock) : ControllerBase
{
    /// <summary>
    /// AcquireLock
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> AcquireLock()
    {
        var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
        return new
        {
            锁ID = acq.AcquireId.ToString(),
            实际值 = acq.Acquired,
            预期值 = true
        };
    }

    /// <summary>
    /// Acquire_And_Block
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> Acquire_And_Block()
    {
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        return new
        {
            锁ID1 = acq1.AcquireId.ToString(),
            实际值1 = acq1.Acquired,
            预期值1 = true,
            实际值2 = acq2.Acquired,
            预期值2 = false
        };
    }

    /// <summary>
    /// Acquire_Block_Release_And_Acquire
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> Acquire_Block_Release_And_Acquire()
    {
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        var r1 = acq1.Acquired;
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(3));
        var r2 = acq2.Acquired;
        await mongoLock.ReleaseAsync(acq1);
        var acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        var r3 = acq3.Acquired;
        return new
        {
            锁ID1 = acq1.AcquireId.ToString(),
            实际值1 = r1,
            预期值1 = true,
            实际值2 = r2,
            预期值2 = false,
            锁ID3 = acq3.AcquireId.ToString(),
            实际值3 = r3,
            预期值3 = true
        };
    }

    /// <summary>
    /// Acquire_BlockFor5Seconds_Release_Acquire
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> Acquire_BlockFor5Seconds_Release_Acquire()
    {
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        var r1 = acq1.Acquired;
        var acq2 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 4000, 6000);
        var r2 = acq2.Acquired;
        await mongoLock.ReleaseAsync(acq1);
        var acq3 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 0, 1500);
        var r3 = acq3.Acquired;
        return new
        {
            锁ID1 = acq1.AcquireId.ToString(),
            实际值1 = r1,
            预期值1 = true,
            实际值2 = r2,
            预期值2 = false,
            锁ID3 = acq3.AcquireId.ToString(),
            实际值3 = r3,
            预期值3 = true
        };
    }

    /// <summary>
    /// Acquire_WaitUntilExpire_Acquire
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> Acquire_WaitUntilExpire_Acquire()
    {
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        var r1 = acq1.Acquired;
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        var r2 = acq2.Acquired;
        await Task.Delay(TimeSpan.FromSeconds(15));
        var acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        var r3 = acq3.Acquired;
        return new
        {
            锁ID1 = acq1.AcquireId.ToString(),
            实际值1 = r1,
            预期值1 = true,
            实际值2 = r2,
            预期值2 = false,
            锁ID3 = acq3.AcquireId.ToString(),
            实际值3 = r3,
            预期值3 = true
        };
    }

    /// <summary>
    /// Synchronize_CriticalSection_For_4_Threads
    /// </summary>
    [HttpGet]
    public dynamic Synchronize_CriticalSection_For_4_Threads()
    {
        var tasks = new List<Task>();
        var bucket = new List<int> { 0 };
        var random = new Random(DateTime.UtcNow.GetHashCode());
        for (var i = 0; i < 4; i++)
        {
            tasks.Add(Task.Run(async delegate
            {
                for (var j = 0; j < 100; j++)
                {
                    var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(10));
                    try
                    {
                        var count = bucket.Count;
                        Thread.Sleep(random.Next(0, 10));
                        var value = bucket[count - 1];
                        bucket.Add(value + 1);
                    }
                    finally
                    {
                        await mongoLock.ReleaseAsync(acq);
                    }
                }
            }));
        }
        Task.WaitAll(tasks.ToArray());
        var result = bucket.SequenceEqual(Enumerable.Range(0, 401));
        return new
        {
            实际值1 = result,
            预期值1 = true
        };
    }

    private static async Task<T> InTimeRange<T>(Func<Task<T>> action, double from, double to)
    {
        var started = DateTime.UtcNow;
        var result = await action.Invoke();
        var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
        return !(elapsed <= to && elapsed >= from) ? throw new("有错") : result;
    }
}