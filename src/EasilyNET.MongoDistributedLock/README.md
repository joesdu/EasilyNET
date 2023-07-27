#### EasilyNET.MongoDistributedLock

- 基于 [GitHub开源项目](https://github.com/gritse/mongo-lock)

##### 使用方法

```csharp
// 使用MongoDB驱动创建一个链接
var setting = new MongoClientSettings
{
    Servers = new List<MongoServerAddress> { new("127.0.0.1", 27018) },
    Credential = MongoCredential.CreateCredential("admin", "guest", "guest"),
    LinqProvider = LinqProvider.V3
};
var client = new MongoClient(setting);
var db = client.GetDatabase("locks");
try
{
    // 由于使用到一些特性.需要将这个集合设置成 上限集合
    db.CreateCollection("release.signal", new()
    {
        // 这个数量理论上可以决定同时系统能有多少个锁.
        MaxDocuments = 100,
        MaxSize = 4096,
        Capped = true
    });
}
catch
{
    // ignored
}
IMongoCollection<LockAcquire> _locks = db.GetCollection<LockAcquire>("lock.acquire");
IMongoCollection<ReleaseSignal> _signals = db.GetCollection<ReleaseSignal>("release.signal");

// 获取锁
var mongoLock = new DistributedLock(_locks, _signals, ObjectId.GenerateNewId());
var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));

// 释放锁 也可以等超时释放
await mongoLock.ReleaseAsync(acq1);
```

- 实际用的时候大概是这样.
```csharp
try
{
    if (acq.Acquired)
    {
        // 关键部分,它不能一次由任何服务器上的多个线程执行
        // ...
        // ...
    }
    else
    {
        // 超时!也许另一个线程没有释放锁...我们可以再试一次或抛出例外
    }
}
finally
{
    // 如果（acq.Acquired）无需手动操作
    await mongoLock.ReleaseAsync(acq);
}
```

- **注意事项和工作原理**
 1. 当您尝试获取锁时,具有指定 lockId 的文档将添加到锁集合中,或者更新(如果存在).
 1. 释放锁时,将更新文档,并将新文档添加到信号上限集合中
 1. 当锁定正在等待时,将使用服务器端等待的可尾游标.[详细信息](https://docs.mongodb.com/manual/reference/method/cursor.tailable)
 1. 生存期是锁有效的时间段.在此时间之后,锁将自动“释放”,并且可以再次获取.它可以防止死锁.
 1. 不要使用长时间的超时,这可能会引发MongoDB驱动程序的异常.正常超时不超过 1-2 分钟!