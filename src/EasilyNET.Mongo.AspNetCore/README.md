### EasilyNET.Mongo.AspNetCore

一个强大的 MongoDB 驱动服务包，为 ASP.NET Core 应用提供便捷的 MongoDB 数据库操作和 GridFS 文件存储支持。

---

### **核心特性**

#### 1. **智能字段映射**

- **驼峰命名转换**: 自动将 C# PascalCase 字段转为数据库中的 camelCase
- **ID 字段映射**: 自动将 `_id` 映射到实体中的 `Id` 或 `ID` 字段，反之亦然
- **灵活类型配置**: 可配置特定类的 Id 字段存储为 string 而非 ObjectId

#### 2. **现代类型支持**

- **.NET 6+ 类型**: 完整支持 `DateOnly` 和 `TimeOnly`
- **多种序列化方案**: 支持字符串格式或 Ticks (long) 存储
- **动态类型**: 支持 `object`、`dynamic` 和匿名类型
- **JSON 类型**: 支持 `JsonNode` 和 `JsonObject`
- **枚举字典**: 支持以枚举为键的字典类型

#### 3. **时间类型本地化**

- 自动本地化 MongoDB 的 DateTime 类型
- 默认将 DateTime 序列化为本地时间（DateTimeKind.Local）
- Decimal 类型自动序列化为 Decimal128

#### 4. **索引管理**

- 支持通过特性方式声明索引
- 自动创建和更新索引
- 支持复合索引、唯一索引、文本索引等

#### 5. **GridFS 分布式文件系统**

- 完整的断点续传文件上传/下载
- HTTP Range 请求支持，完美支持视频/音频流式播放
- 分块上传优化（默认 255KB），提升流式传输性能
- 秒传功能（基于文件哈希去重）
- 自动清理过期会话
- 内置 REST API 控制器

#### 6. **APM 监控支持**

- 集成 SkyAPM 探针支持
- 支持自定义事件订阅器

---

### **安装**

通过 NuGet 安装：

```bash
dotnet add package EasilyNET.Mongo.AspNetCore
```

---

### **快速开始**

#### 配置连接字符串

在 `appsettings.json` 或环境变量中配置：

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017/your-database"
  }
}
```

或使用环境变量：

```bash
CONNECTIONSTRINGS_MONGO=mongodb://localhost:27017/your-database
```

---

### **MongoDB Context 配置**

#### 方式 1: 使用 IConfiguration (推荐)

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加 MongoDB 数据库服务
builder.Services.AddMongoContext<DbContext>(builder.Configuration, c =>
{
    // 配置数据库名称（可选，覆盖连接字符串中的数据库名）
    c.DatabaseName = "your-database";

    // 配置不需要将 Id 字段存储为 ObjectId 的类型
    // 使用 $unwind 操作符时，ObjectId 转换可能有问题，可调整为字符串
    c.ObjectIdToStringTypes = new()
    {
        typeof(YourEntityType)
    };

    // 是否使用默认转换配置（推荐启用）
    // 包含以下功能：
    // 1. 小驼峰字段名称，如: pageSize, linkPhone
    // 2. 忽略代码中未定义的字段
    // 3. 将 ObjectId 字段 _id 映射到实体中的 ID 或 Id 字段
    // 4. 将枚举类型存储为字符串，如: Gender.男 存储为 "男" 而非 int
    c.DefaultConventionRegistry = true;

    // 配置自定义 Convention（可选）
    c.ConventionRegistry = new()
    {
        {
            "custom-convention",
            new ConventionPack { new IgnoreIfDefaultConvention(true) }
        }
    };

    // 通过 ClientSettings 配置特殊功能（可选）
    c.ClientSettings = cs =>
    {
        // 对接 SkyAPM 的 MongoDB 探针
        cs.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());

        // 其他高级配置...
    };
});

var app = builder.Build();
app.Run();
```

#### 方式 2: 使用连接字符串

```csharp
builder.Services.AddMongoContext<DbContext>(
    "mongodb://localhost:27017/test-db",
    c =>
{
    c.DatabaseName = "test-db";
    c.DefaultConventionRegistry = true;
});
```

#### 方式 3: 使用 MongoClientSettings

```csharp
builder.Services.AddMongoContext<DbContext>(
    new MongoClientSettings
    {
        Servers = new List<MongoServerAddress>
        {
            new("127.0.0.1", 27017)
        },
        Credential = MongoCredential.CreateCredential("admin", "username", "password"),
        ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber())
    },
    c =>
    {
        c.DatabaseName = "test-db";
        c.DefaultConventionRegistry = true;
    }
);
```

---

### **自定义序列化器**

#### DateOnly / TimeOnly 序列化

支持两种存储方式：

**1. 字符串格式（推荐，便于人类阅读）**

```csharp
// 使用默认格式（yyyy-MM-dd 和 HH:mm:ss）
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());

// 使用自定义格式
builder.Services.RegisterSerializer(new DateOnlySerializerAsString("yyyy/MM/dd"));
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString("HH:mm:ss.fff"));
```

**2. Ticks 格式（long 类型，节省空间）**

```csharp
builder.Services.RegisterSerializer(new DateOnlySerializerAsTicks());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsTicks());
```

⚠️ **注意**: 同一类型全局只能注册一种序列化方案，String 和 Ticks 方式会冲突。

#### JsonNode / JsonObject 支持

```csharp
builder.Services.RegisterSerializer(new JsonNodeSerializer());
builder.Services.RegisterSerializer(new JsonObjectSerializer());
```

> ⚠️ JsonNode 反序列化不支持 Unicode 字符。如需序列化到 Redis 等其他存储，需要将 `JsonSerializerOptions.Encoder` 设置为 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。

#### 动态类型支持

```csharp
// 支持 object、dynamic 和匿名类型
builder.Services.RegisterDynamicSerializer();
```

#### 枚举键字典支持

```csharp
// 支持 Dictionary<TEnum, TValue> 类型
builder.Services.RegisterGlobalEnumKeyDictionarySerializer();
```

#### 其他自定义序列化器

```csharp
// Double 类型
builder.Services.RegisterSerializer(new DoubleSerializer(BsonType.Double));

// Guid 类型
builder.Services.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
```

---

### **使用 EasilyNET.AutoDependencyInjection 集成**

#### 1. 安装依赖包

```bash
dotnet add package EasilyNET.AutoDependencyInjection
```

#### 2. 创建 Mongo 模块

```csharp
public class MongoModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();

        // 方式 1: 使用 IConfiguration
        context.Services.AddMongoContext<DbContext>(config, c =>
        {
            c.DatabaseName = "test-db";
            c.DefaultConventionRegistry = true;
            c.ObjectIdToStringTypes = new() { typeof(SomeEntity) };
        });

        // 注册序列化器
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());

        await base.ConfigureServices(context);
    }
}
```

#### 3. 创建根模块

```csharp
[DependsOn(
    typeof(DependencyAppModule),
    typeof(MongoModule)
)]
public class AppWebModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddHttpContextAccessor();
        await base.ConfigureServices(context);
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseAuthorization();
        await base.ApplicationInitialization(context);
    }
}
```

#### 4. Program.cs 配置

```csharp
var builder = WebApplication.CreateBuilder(args);

// 注册模块系统
builder.Services.AddApplicationModules<AppWebModule>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// 初始化模块
app.InitializeApplication();

app.MapControllers();
app.Run();
```

---

### **GridFS 分布式文件系统**

GridFS 是 MongoDB 的分布式文件系统，支持存储超过 16MB 的大文件。本实现经过优化，支持高效的流式传输和断点续传。

#### 核心特性

- ✅ **断点续传**: 分块上传，支持暂停/恢复
- ✅ **秒传功能**: 基于文件哈希（SHA256）自动去重
- ✅ **流式传输**: 支持 HTTP Range 请求，完美支持视频/音频播放
- ✅ **自动清理**: 自动清理过期的上传会话
- ✅ **性能优化**: 默认 255KB 分块大小，优化流式性能
- ✅ **REST API**: 内置完整的上传/下载 API

#### 快速配置

**方式 1: 使用容器中的 IMongoDatabase（推荐）**

```csharp
// 需要先注册 MongoContext
builder.Services.AddMongoContext<DbContext>(builder.Configuration);

// 添加 GridFS 支持
builder.Services.AddMongoGridFS(options =>
{
    options.BucketName = "fs";           // 自定义 Bucket 名称
    options.ChunkSizeBytes = 255 * 1024; // 255KB，优化流式性能
}, serverOptions =>
{
    serverOptions.EnableController = true; // 是否启用内置 API（默认 true）

    // 可选：添加授权策略
    // serverOptions.AuthorizeData.Add(new AuthorizeAttribute { Policy = "FileUpload" });
});
```

**方式 2: 使用 IConfiguration**

```csharp
builder.Services.AddMongoGridFS(
    builder.Configuration,
    options =>
    {
        options.ChunkSizeBytes = 255 * 1024;
    }
);
```

**方式 3: 使用 MongoClientSettings**

```csharp
builder.Services.AddMongoGridFS(
    new MongoClientSettings
    {
        Servers = new List<MongoServerAddress> { new("127.0.0.1", 27017) }
    },
    dbName: "test-db",
    configure: options =>
    {
        options.ChunkSizeBytes = 255 * 1024;
    }
);
```

#### 内置 REST API

启用 GridFS 后，自动注册以下 API 端点：

| 端点                                    | 方法   | 说明                         |
| --------------------------------------- | ------ | ---------------------------- |
| `POST /api/GridFS/CreateSession`        | POST   | 创建上传会话（支持秒传检测） |
| `POST /api/GridFS/UploadChunk`          | POST   | 上传文件块                   |
| `GET /api/GridFS/Session/{sessionId}`   | GET    | 获取会话信息                 |
| `GET /api/GridFS/MissingChunks/{id}`    | GET    | 获取缺失的块编号             |
| `POST /api/GridFS/Finalize/{sessionId}` | POST   | 完成上传                     |
| `DELETE /api/GridFS/Cancel/{sessionId}` | DELETE | 取消上传会话                 |
| `GET /api/GridFS/Download/{fileId}`     | GET    | 下载文件（支持 Range）       |
| `GET /api/GridFS/Info/{fileId}`         | GET    | 获取文件信息                 |
| `DELETE /api/GridFS/Delete/{fileId}`    | DELETE | 删除文件                     |
| `GET /api/GridFS/StorageStats`          | GET    | 获取存储统计信息             |

#### 控制器配置

可以配置授权策略和过滤器：

```csharp
builder.Services.AddMongoGridFS(
    builder.Configuration,
    serverConfigure: options =>
    {
        // 禁用内置控制器（如果需要自定义实现）
        options.EnableController = false;

        // 添加授权策略
        options.AuthorizeData.Add(new AuthorizeAttribute
        {
            Policy = "FileUpload"
        });

        // 添加自定义过滤器
        options.Filters.Add(new CustomActionFilter());
    }
);
```

#### 使用 JavaScript SDK

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
