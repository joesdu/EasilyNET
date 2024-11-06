using EasilyNET.Core.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Abstraction;
using MongoDB.Bson;

namespace WebApi.Test.Unit.Controllers;

/// <summary>
/// MongoDB分布式锁测试
/// </summary>
[Route("api/[controller]/[action]")]
[ApiController]
[ApiGroup("MongoLock", "基于MongoDB实现的分布式锁测试")]
public class MongoLockController(IMongoLockFactory lockFactory, DbContext db) : ControllerBase
{
    /// <summary>
    /// 测试锁
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task BusinessTest()
    {
        const string lockId = "64d44afda4473b85a177084d"; // 这里使用一个随机的ID作为锁ID,相当于其他锁中的Key.用来区分不同的业务的锁
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.Parse(lockId));
        var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
        try
        {
            if (acq.Acquired)
            {
                await db.GetCollection<dynamic>("locks_test").InsertOneAsync(new
                {
                    AcquiredId = acq.AcquireId
                });
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await mongoLock.ReleaseAsync(acq);
        }
    }

    /// <summary>
    /// AcquireLock
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<dynamic> AcquireLock()
    {
        const string lockId = "64d44afde4473b85a177084c"; // 这里使用一个随机的ID作为锁ID,相当于其他锁中的Key.用来区分不同的业务的锁
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.Parse(lockId));
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
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.GenerateNewId()); // 这里偷懒.就临时生成一个ID,实际业务中应该每一个业务对应一个ID
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
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.GenerateNewId()); // 这里偷懒.就临时生成一个ID,实际业务中应该每一个业务对应一个ID
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
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.GenerateNewId()); // 这里偷懒.就临时生成一个ID,实际业务中应该每一个业务对应一个ID
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
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.GenerateNewId()); // 这里偷懒.就临时生成一个ID,实际业务中应该每一个业务对应一个ID
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
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.GenerateNewId()); // 这里偷懒.就临时生成一个ID,实际业务中应该每一个业务对应一个ID
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
        Task.WaitAll(tasks);
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