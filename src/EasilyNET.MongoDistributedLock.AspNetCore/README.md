#### EasilyNET.MongoDistributedLock.AspNetCore

- EasilyNET.MongoDistributedLock.AspNetCore 让基于 EasilyNET.MongoDistributedLock 的使用更简单.

- 注册分布式锁服务,用来配置一些基本信息

```csharp
// 使用默认配置
builder.Services.AddMongoDistributedLock();

// 自定义一些配置
builder.Services.AddMongoDistributedLock(op =>
{
    op.DatabaseName = "test_locks";
    op.MaxDocument = 100;
    ...
});
```

- 使用 IMongoLockFactory 来注入工厂服务

```csharp
public class DistributedLockTest(IMongoLockFactory lockFactory)
{
    public async Task<dynamic> AcquireLock()
    {
        // 这里使用一个随机的ID作为锁ID,相当于其他锁中的Key.用来区分不同的业务的锁,也可以将不同的业务类型放到MongoDB中存起来,然后再使用的时候再取获取这个id
        const string lockId = "64d44afde4473b85a177084c";
        var mongoLock = lockFactory.GenerateNewLock(ObjectId.Parse(lockId));

        var acq = await mongoLock.AcquireAsync(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(0));
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
    }
}
```
