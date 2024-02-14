### EasilyNET.MongoGridFS.AspNetCore

- 从 Minio.AspNetCore 得到灵感.创建一个 MongoDBGridFS 的服务注册方案.

#### 使用方法

- 注册服务

```csharp
var db = builder.Services.GetService<IMongoDatabase>() ?? throw new("请先注册IMongoDatabase服务");
builder.Services.AddMongoGridFS(db);
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
