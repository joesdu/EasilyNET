##### 如何使用?

- 在系统环境变量或者 Docker 容器中设置环境变量名称为: CONNECTIONSTRINGS_MONGO = mongodb 链接字符串 或者在
  appsetting.json 中添加,
- 现在你也可以参考 Test 项目查看直接传入相关数据.

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb链接字符串"
  },
  // 或者使用
  "CONNECTIONSTRINGS_MONGO": "mongodb链接字符串"
}
```

##### 使用 EasilyNET.Mongo.GridFS

- 使用 Nuget 安装 EasilyNET.Mongo.GridFS
- .Net 6 +
- 已配置Kestrel和IIS的最大文件限制,无需再次配置

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// 添加Mongodb数据库服务
builder.Services.AddMongoContext<EasilyNETMongoContext>(builder.Configuration);
// 添加GridFS服务
builder.Services.AddEasilyNETGridFS(fsOptions: c =>
{
    c.BusinessApp = "easilyfs";
    c.Options = new()
    {
        BucketName = "easilyfs",
        ChunkSizeBytes = 1024,
        DisableMD5 = true,
        ReadConcern = new() { },
        ReadPreference = ReadPreference.Primary,
        WriteConcern = WriteConcern.Unacknowledged
    };
    c.DefaultDB = true;
    c.ItemInfo = "item.info";
});
...

var app = builder.Build();
```

##### 使用 EasilyNET.Mongo.GridFS.Extension

- 配置appsettings.json

```json
{
  // 添加文件缓存
  "EasilyFS": {
    "VirtualPath": "/easilyfs",
    "PhysicalPath": "/home/username/test"
  }
}
```

- 新增文件缓存到物理路径,便于文件在线使用.
- 添加物理路径清理接口.(可通过调用该接口定时清理所有缓存的文件)

---

- 使用 Nuget 安装 EasilyNET.Mongo.GridFS.Extension
- .Net 6 +

```csharp
var app = builder.Build();
...

// 添加虚拟目录用于缓存文件,便于在线播放等功能.
app.UseGridFSVirtualPath(builder.Configuration);

...

app.Run();
```

- 使用EasilyNET.Mongo.GridFS.Extension后建议使用Extension控制器中的接口,原GridFS中的接口就可以不使用了.