### EasilyNET.Mongo.AspNetCore

一个强大的 MongoDB 驱动服务包，为 ASP.NET Core 应用提供便捷的 MongoDB 数据库操作支持。

#### 核心特性

- **字段命名转换**: 数据库中字段名自动驼峰命名，ID/Id 字段自动转换为 ObjectId
- **灵活 ID 配置**: 可配置部分类的 Id 字段存储为 string 类型而非 ObjectId，支持子对象和集合成员
- **时间类型本地化**: 自动本地化 MongoDB 时间类型
- **.NET 6+ 支持**: 添加 DateOnly/TimeOnly 类型支持，可序列化为 String 或 long
- **索引管理**: 支持通过特性方式自动创建和更新索引
- **GridFS 文件存储**: 完整的文件存储解决方案

## 📋 更新日志 (ChangeLogs)

- **自定义格式化**: 支持自定义 TimeOnly 和 DateOnly 的格式化格式
  - 支持转换为字符串格式存储
  - 支持转换为 Ticks (long) 方式存储
  - 可自定义实现其他类型转换，如 ulong
- **动态类型支持**: 添加 object 和 dynamic 类型支持 (2.20 版本后官方已支持 JsonArray)
- **JsonNode 支持**: 添加 JsonNode 和 JsonObject 类型支持

##### 添加自定义序列化支持(可选)

-

JsonNode 类型因为反序列化时不支持 Unicode 字符，如果需要序列化插入至其他地方（例如 Redis），在序列化时需要将
JsonSerializerOptions 的 Encoder 属性设置为 System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping.

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration)
// 添加自定义序列化
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());
// 或者将他们存储为long类型的Ticks,也可以自己组合使用.
builder.Services.RegisterSerializer(new DateOnlySerializerAsTicks());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsTicks());
// 添加JsonNode支持
builder.Services.RegisterSerializer(new JsonNodeSerializer());
builder.Services.RegisterSerializer(new JsonObjectSerializer());
```

## 🚀 快速开始

### 安装

通过 NuGet 安装 EasilyNET.Mongo.AspNetCore：

```bash
dotnet add package EasilyNET.Mongo.AspNetCore
```

### 配置连接字符串

在系统环境变量、Docker 容器或 `appsettings.json` 中设置 MongoDB 连接字符串：

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017/your-database"
  }
}
```

或者使用环境变量：

```bash
CONNECTIONSTRINGS_MONGO=mongodb://localhost:27017/your-database
```

### APM 监控支持

支持 APM 探针监控，基于 [SkyAPM.Diagnostics.MongoDB](https://github.com/SkyAPM/SkyAPM-dotnet/tree/main/src/SkyApm.Diagnostics.MongoDB)。

---

## 📖 使用方法

### 方法 1: 使用默认依赖注入

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加 MongoDB 数据库服务
builder.Services.AddMongoContext<DbContext>(builder.Configuration, c =>
{
    // 配置数据库名称，覆盖连接字符串中的数据库名称
    c.DatabaseName = "your-database";

    // 配置不需要将 Id 字段存储为 ObjectId 的类型
    // 使用 $unwind 操作符时，ObjectId 在转换上会有问题，所以调整为字符串
    c.ObjectIdToStringTypes = new()
    {
        typeof(YourEntityType)
    };

    // 是否使用默认转换配置，包含以下内容：
    // 1. 小驼峰字段名称，如: pageSize, linkPhone
    // 2. 忽略代码中未定义的字段
    // 3. 将 ObjectId 字段 _id 映射到实体中的 ID 或 Id 字段，反之亦然
    // 4. 将枚举类型存储为字符串，如: Gender.男 存储为 "男" 而非 int 类型
    c.DefaultConventionRegistry = true;

    // 配置自定义 Convention
    c.ConventionRegistry = new()
    {
        {
            $"{SnowId.GenerateNewId()}",
            new() { new IgnoreIfDefaultConvention(true) }
        }
    };

    // 通过 ClientSettings 配置特殊功能
    c.ClientSettings = cs =>
    {
        // 对接 SkyAPM 的 MongoDB 探针或其他事件订阅器
        cs.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventSubscriber());
    };
});

// 添加 .NET 6+ 新 TimeOnly 和 DateOnly 数据类型的序列化方案
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());

// 注册其他序列化方案
builder.Services.RegisterSerializer(new DoubleSerializer(BsonType.Double));

var app = builder.Build();
```

### 方法 2: 使用 EasilyNET.AutoDependencyInjection

1. **安装依赖包**:

   ```bash
   dotnet add package EasilyNET.AutoDependencyInjection
   ```

2. **创建 EasilyNETMongoModule.cs**:

```csharp
public class EasilyNETMongoModule : AppModule
{
    /// <summary>
    /// 配置和注册服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        // 使用 IConfiguration 的方式注册例子,使用链接字符串,仅需将config替换成连接字符即可.
        //context.Services.AddMongoContext<DbContext>(config, c =>
        //{
        //    // 配置数据库名称,覆盖掉连接字符串中的数据库名称
        //    c.DatabaseName = "test23";
        //    // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题,所以需要将其调整为字符串.
        //    c.ObjectIdToStringTypes = new()
        //    {
        //        typeof(MongoTest2)
        //    };
        //    // 是否使用默认转换配置.包含如下内容:
        //    // 1.小驼峰字段名称 如: pageSize ,linkPhone
        //    // 2.忽略代码中未定义的字段
        //    // 3.将ObjectID字段 _id 映射到实体中的ID或者Id字段,反之亦然.在存入数据的时候将Id或者ID映射为 _id
        //    // 4.将枚举类型存储为字符串, 如: Gender.男 存储到数据中为 男,而不是 int 类型
        //    c.DefaultConventionRegistry = true;
        //    c.ConventionRegistry= new()
        //    {
        //        {
        //            $"{SnowId.GenerateNewId()}",
        //            new() { new IgnoreIfDefaultConvention(true) }
        //        }
        //    };
        //    // 通过ClientSettings来配置一些使用特殊的东西
        //    c.ClientSettings = cs =>
        //    {
        //        // 对接 SkyAPM 的 MongoDB探针或者别的事件订阅器
        //        cs.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventSubscriber());
        //    };
        //});
        //context.Services.AddMongoContext<DbContext2>(config);
        //context.Services.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));

        // 例子二:使用MongoClientSettings配置
        context.Services.AddMongoContext<DbContext>(new MongoClientSettings
        {
            Servers = new List<MongoServerAddress> { new("127.0.0.1", 27018) },
            Credential = MongoCredential.CreateCredential("admin", "guest", "guest"),
            // 对接 SkyAPM 的 MongoDB探针
            ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber())
        }, c =>
        {
            // 配置数据库名称,覆盖掉连接字符串中的数据库名称
            c.DatabaseName = "test23";
            // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题.
            c.ObjectIdToStringTypes = new()
            {
                typeof(MongoTest2)
            };
            // 是否使用默认转换配置.包含如下内容:
            // 1.小驼峰字段名称 如: pageSize ,linkPhone
            // 2.忽略代码中未定义的字段
            // 3.将ObjectID字段 _id 映射到实体中的ID或者Id字段,反之亦然.在存入数据的时候将Id或者ID映射为 _id
            // 4.将枚举类型存储为字符串, 如: Gender.男 存储到数据中为 男,而不是 int 类型
            c.DefaultConventionRegistry = true;
            c.ConventionRegistry= new()
            {
                {
                    $"{SnowId.GenerateNewId()}",
                    new() { new IgnoreIfDefaultConvention(true) }
                }
            };
        });
        // 注册另一个DbContext
        context.Services.AddMongoContext<DbContext2>(config, c =>
        {
            c.DefaultConventionRegistry = true;
            c.ConventionRegistry = new()
            {
                {
                    $"{SnowId.GenerateNewId()}",
                    new() { new IgnoreIfDefaultConvention(true) }
                }
            };
        });
    }
}
```

- 创建 AppWebModule.cs 并添加 EasilyNETMongoModule

```csharp
/**
 * 要实现自动注入,一定要在这个地方添加
 */
[DependsOn(
    typeof(DependencyAppModule),
    typeof(EasilyNETMongoModule)
)]
public class AppWebModule : AppModule
{
    /// <summary>
    /// 注册和配置服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        base.ConfigureServices(context);
        _ = context.Services.AddHttpContextAccessor();
    }
    /// <summary>
    /// 注册中间件
    /// </summary>
    /// <param name="context"></param>
    public override void ApplicationInitialization(ApplicationContext context)
    {
        base.ApplicationInitialization(context);
        var app = context.GetApplicationBuilder();
        _ = app.UseAuthorization();
    }
}
```

- 最后在 Program.cs 中添加如下内容

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// 自动注入服务模块
builder.Services.AddApplication<AppWebModule>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) _ = app.UseDeveloperExceptionPage();

// 添加自动化注入的一些中间件.
app.InitializeApplication();

app.MapControllers();

app.Run();
```

---

## 📁 GridFS 文件存储

GridFS 是 MongoDB 的分布式文件系统,支持存储超过 16MB 的文件.本实现经过优化,支持高效的流式传输和范围读取.

### 基础使用

1. **注册服务**:

```csharp
// 需要提前注册 IMongoDatabase，或使用其他重载
builder.Services.AddMongoGridFS(options =>
{
    options.ChunkSizeBytes = 255 * 1024; // 255KB - 优化流式传输性能
});
```

### 🎬 流式传输 - 视频/音频播放

- 支持 HTTP Range 请求的流式传输,完美支持(音)视频播放器的进度拖动和断点续传.
- 支持超大文件的分块上传和断点续传,适合不稳定网络环境.前后端配合实现真正的断点续传.

##### 使用 JavaScript SDK

```javascript
import {
  GridFSUploader,
  GridFSDownloader,
  formatFileSize,
} from "./easilynet-gridfs-sdk.js";

// 上传示例
const startUpload = async (file) => {
  const uploader = new GridFSUploader(file, {
    // url: 'https://api.example.com', // 可选: 如果后端不在当前域,请填写域名
    chunkSize: 1024 * 1024, // 1MB
    maxConcurrent: 3,
    onProgress: (progress) => {
      console.log(`上传进度: ${progress.percentage}%`);
      console.log(`速度: ${formatFileSize(progress.speed)}/s`);
    },
    onError: (error) => {
      console.error("上传错误:", error);
    },
    onComplete: (fileId) => {
      console.log("上传完成, FileId:", fileId);
    },
  });

  try {
    await uploader.start();
  } catch (error) {
    console.error("上传失败:", error);
  }

  // 支持暂停/恢复/取消
  // uploader.pause();
  // await uploader.resume();
  // await uploader.cancel();
};

// 下载示例
const startDownload = async (fileId) => {
  const downloader = new GridFSDownloader({
    fileId: fileId,
    onProgress: (progress) => {
      console.log(`下载进度: ${progress.percentage}%`);
    },
    onError: (error) => {
      console.error("下载错误:", error);
    },
  });

  try {
    await downloader.downloadAndSave();
  } catch (error) {
    console.error("下载失败:", error);
  }
};
```