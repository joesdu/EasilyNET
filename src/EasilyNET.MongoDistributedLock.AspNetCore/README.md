#### EasilyNET.MongoDistributedLock.AspNetCore

- EasilyNET.MongoDistributedLock.AspNetCore 让基于 EasilyNET.MongoDistributedLock 的使用更简单.

- 注册服务
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

- 使用
```csharp
public class DistributedLockTest(IDistributedLock mongoLock)
{
    public async Task<dynamic> AcquireLock()
    {
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