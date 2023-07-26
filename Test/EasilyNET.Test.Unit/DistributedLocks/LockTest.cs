using EasilyNET.MongoDistributedLock.Core;
using FluentAssertions;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace EasilyNET.Test.Unit.DistributedLocks;

/// <summary>
/// 测试
/// </summary>
[TestClass]
public class LockTests
{
    private static readonly IMongoDatabase db;

    static LockTests()
    {
        var setting = new MongoClientSettings
        {
            Servers = new List<MongoServerAddress> { new("127.0.0.1", 27018) },
            Credential = MongoCredential.CreateCredential("admin", "guest", "guest"),
            LinqProvider = LinqProvider.V3
        };
        var client = new MongoClient(setting);
        db = client.GetDatabase("lock");
    }

    /// <summary>
    /// AcquireLock
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task AcquireLock()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
        var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
        acq.Acquired.Should().BeTrue();
    }

    /// <summary>
    /// Acquire_And_Block
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Acquire_And_Block()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq1.Acquired.Should().BeTrue();
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq2.Acquired.Should().BeFalse();
    }

    /// <summary>
    /// Acquire_Block_Release_And_Acquire
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Acquire_Block_Release_And_Acquire()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq1.Acquired.Should().BeTrue();
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq2.Acquired.Should().BeFalse();
        await mongoLock.ReleaseAsync(acq1);
        var acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq3.Acquired.Should().BeTrue();
    }

    /// <summary>
    /// Acquire_BlockFor5Seconds_Release_Acquire
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Acquire_BlockFor5Seconds_Release_Acquire()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(0));
        acq1.Acquired.Should().BeTrue();
        var acq2 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 4000, 6000);
        acq2.Acquired.Should().BeFalse();
        await mongoLock.ReleaseAsync(acq1);
        var acq3 = await InTimeRange(() => mongoLock.AcquireAsync(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(5)), 0, 1500);
        acq3.Acquired.Should().BeTrue();
    }

    /// <summary>
    /// Acquire_WaitUntilExpire_Acquire
    /// </summary>
    /// <returns></returns>
    [TestMethod]
    public async Task Acquire_WaitUntilExpire_Acquire()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
        var acq1 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        acq1.Acquired.Should().BeTrue();
        var acq2 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        acq2.Acquired.Should().BeFalse();
        await Task.Delay(TimeSpan.FromSeconds(10));
        var acq3 = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0));
        acq3.Acquired.Should().BeTrue();
    }

    /// <summary>
    /// Synchronize_CriticalSection_For_4_Threads
    /// </summary>
    [TestMethod]
    public void Synchronize_CriticalSection_For_4_Threads()
    {
        var mongoLock = new DistributedLock(db, ObjectId.GenerateNewId());
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
        bucket.SequenceEqual(Enumerable.Range(0, 401)).Should().BeTrue();
    }

    private static async Task<T> InTimeRange<T>(Func<Task<T>> action, double from, double to)
    {
        var started = DateTime.UtcNow;
        var result = await action.Invoke();
        var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
        (elapsed <= to && elapsed >= from).Should().BeTrue();
        return result;
    }
}