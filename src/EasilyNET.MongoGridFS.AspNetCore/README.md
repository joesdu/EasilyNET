### EasilyNET.MongoGridFS.AspNetCore



#### 使用方法

- 注册服务

```csharp
// 需要提前注册 IMongoDatabase, 或者使用其他重载来注册服务.
builder.Services.AddMongoGridFS();
```

- 使用依赖注入获取 GridFSBucket 操作 GridFS

```csharp
public class YourClass(IGridFSBucket bucket)
{
    private readonly IGridFSBucket _bucket = bucket;

    public void DoSomething()
    {
        _bucket.XXXXXX();
    }
}
```
