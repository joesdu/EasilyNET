## EasilyNET.Mongo.AspNetCore

为 ASP.NET Core 应用提供完整的 MongoDB 集成方案，涵盖连接注册、序列化、自动索引（含 Atlas Search / Vector Search）、变更流、GridFS 文件存储、固定集合/时序集合自动创建、健康检查等开箱即用能力。

---

## 目录

- [安装](#安装)
- [快速开始：注册 MongoContext](#快速开始注册-mongocontext)
  - [方式 1：使用 IConfiguration（推荐）](#方式-1使用-iconfiguration推荐)
  - [方式 2：使用连接字符串](#方式-2使用连接字符串)
  - [方式 3：使用 MongoClientSettings](#方式-3使用-mongoclientsettings)
  - [弹性连接配置](#弹性连接配置)
- [字段映射与命名约定](#字段映射与命名约定)
- [自定义序列化器](#自定义序列化器)
- [自动创建索引](#自动创建索引)
- [自动创建时序集合](#自动创建时序集合)
- [自动创建固定大小集合](#自动创建固定大小集合)
- [Atlas Search / Vector Search 索引自动创建](#atlas-search--vector-search-索引自动创建)
- [变更流（Change Stream）](#变更流change-stream)
- [GridFS 文件存储](#gridfs-文件存储)
- [健康检查](#健康检查)
- [使用 EasilyNET.AutoDependencyInjection 集成](#使用-easilynetautodependencyinjection-集成)
- [常见问题排查](#常见问题排查)

---

## 安装

```bash
dotnet add package EasilyNET.Mongo.AspNetCore
```

---

## 快速开始：注册 MongoContext

首先继承 `MongoContext` 定义自己的数据库上下文（详见 `EasilyNET.Mongo.Core` 文档）：

```csharp
public class MyDbContext : MongoContext
{
    public IMongoCollection<Order> Orders { get; set; } = default!;
    public IMongoCollection<User> Users { get; set; } = default!;
}
```

### 方式 1：使用 IConfiguration（推荐）

在 `appsettings.json` 中配置连接字符串：

```json
{
  "ConnectionStrings": {
    "Mongo": "mongodb://localhost:27017/mydb"
  }
}
```

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMongoContext<MyDbContext>(builder.Configuration, c =>
{
    // 数据库名称（可选，覆盖连接字符串中的库名）
    c.DatabaseName = "mydb";

    // 启用默认命名约定（强烈推荐）：驼峰字段名 + _id 映射 + 枚举存字符串
    c.DefaultConventionRegistry = true;

    // 特定类型的 ObjectId 存为 string（在使用 $unwind 时有时需要此配置）
    c.ObjectIdToStringTypes = [typeof(SomeEntity)];

    // 追加自定义 Convention（可选）
    c.ConventionRegistry = new()
    {
        { "myConvention", new ConventionPack { new IgnoreIfDefaultConvention(true) } }
    };

    // 高级驱动配置（可选，如对接 APM 探针）
    c.ClientSettings = cs =>
    {
        cs.ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
    };
});
```

### 方式 2：使用连接字符串

```csharp
builder.Services.AddMongoContext<MyDbContext>("mongodb://localhost:27017/mydb", c =>
{
    c.DefaultConventionRegistry = true;
});
```

### 方式 3：使用 MongoClientSettings

```csharp
builder.Services.AddMongoContext<MyDbContext>(
    new MongoClientSettings
    {
        Servers = [new MongoServerAddress("127.0.0.1", 27017)],
        Credential = MongoCredential.CreateCredential("admin", "username", "password")
    },
    c =>
    {
        c.DatabaseName = "mydb";
        c.DefaultConventionRegistry = true;
    });
```

### 弹性连接配置

MongoDB 驱动内置连接自动恢复，`Resilience` 提供开箱即用的默认超时与连接池配置，与驱动自带机制协同工作：

```csharp
builder.Services.AddMongoContext<MyDbContext>(builder.Configuration, c =>
{
    c.DefaultConventionRegistry = true;

    // 启用弹性默认值（与驱动内置自动恢复机制配合）
    c.Resilience.Enable = true;

    // 以下均为可选调整，括号内为默认值
    c.Resilience.ServerSelectionTimeout = TimeSpan.FromSeconds(10); // 服务器选择超时
    c.Resilience.ConnectTimeout = TimeSpan.FromSeconds(10);         // TCP 连接建立超时
    c.Resilience.SocketTimeout = TimeSpan.FromSeconds(60);          // Socket 读写超时
    c.Resilience.WaitQueueTimeout = TimeSpan.FromMinutes(1);        // 连接池等待超时
    c.Resilience.HeartbeatInterval = TimeSpan.FromSeconds(10);      // 心跳检测间隔
    c.Resilience.MaxConnectionPoolSize = 100;                       // 最大连接数
    c.Resilience.MinConnectionPoolSize = null;                      // 最小连接数（null = 使用驱动默认值）
    c.Resilience.RetryReads = true;                                 // 自动重试读操作
    c.Resilience.RetryWrites = true;                                // 自动重试写操作
});
```

**场景化建议**：

| 场景           | 推荐配置                                                |
| -------------- | ------------------------------------------------------- |
| 同区域低延迟   | `ConnectTimeout=5s`, `ServerSelectionTimeout=5s`        |
| 跨区域高延迟   | `ConnectTimeout=20s`, `ServerSelectionTimeout=30s`      |
| 高并发大流量   | `MaxConnectionPoolSize=200+`, `WaitQueueTimeout=30s`    |
| 代理 / 单节点  | 连接串加 `directConnection=true` 或 `loadBalanced=true` |
| Atlas / 云托管 | 保持默认值，确保 `RetryReads=true`, `RetryWrites=true`  |

> ⚠️ 弹性配置不能解决网络/认证/拓扑错误，请先排查连接串配置。

---

## 字段映射与命名约定

启用 `DefaultConventionRegistry = true` 后，框架会自动注册以下规则：

| 功能                | 说明                                                        | 示例                     |
| ------------------- | ----------------------------------------------------------- | ------------------------ |
| **驼峰字段名**      | C# `PascalCase` → MongoDB `camelCase`                       | `PageSize` → `pageSize`  |
| **`_id` 映射**      | 自动将 `_id` 与实体中的 `Id` / `ID` 属性互相映射            | `_id` ↔ `Id`             |
| **枚举存字符串**    | 枚举值以字符串形式存储，便于阅读                            | `Gender.Male` → `"Male"` |
| **忽略未知字段**    | 反序列化时忽略数据库中存在但代码中未定义的字段              | 向前兼容                 |
| **DateTime 本地化** | `DateTime` 反序列化后自动设为 `DateTimeKind.Local`          | 时区一致                 |
| **Decimal128**      | `decimal` 类型自动映射为 MongoDB `Decimal128`，避免精度丢失 | 金额字段                 |

---

## 自定义序列化器

在 `AddMongoContext` 之后，通过扩展方法注册序列化器。

### DateOnly / TimeOnly

官方新版驱动已支持，本库额外提供字符串和 Ticks 两种存储方式以兼容历史数据：

```csharp
// 字符串格式（默认 "yyyy-MM-dd" 和 "HH:mm:ss.ffffff"，便于阅读和查询）
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());

// 自定义格式
builder.Services.RegisterSerializer(new DateOnlySerializerAsString("yyyy/MM/dd"));

// Ticks 格式（long 类型，节省空间，适合高频时间字段）
builder.Services.RegisterSerializer(new DateOnlySerializerAsTicks());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsTicks());
```

> ⚠️ 同一类型全局只能注册一种方案，String 和 Ticks 方式不能同时注册。

### 动态 / JSON 类型

```csharp
// object、dynamic、匿名类型支持
builder.Services.RegisterDynamicSerializer();

// System.Text.Json 的 JsonNode / JsonObject 类型
builder.Services.RegisterSerializer(new JsonNodeSerializer());
builder.Services.RegisterSerializer(new JsonObjectSerializer());
```

> ⚠️ `JsonNode` 反序列化不支持 Unicode 转义字符；若需要跨系统序列化，请将 `JsonSerializerOptions.Encoder` 设为 `JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。

### 枚举键字典

```csharp
// 支持 Dictionary<TEnum, TValue> / IDictionary<TEnum, TValue> / IReadOnlyDictionary<TEnum, TValue>
builder.Services.RegisterGlobalEnumKeyDictionarySerializer();
```

### 其他

```csharp
builder.Services.RegisterSerializer(new DoubleSerializer(BsonType.Double));
builder.Services.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
```

---

## 自动创建索引

通过 `[MongoIndex]` / `[MongoCompoundIndex]` 特性声明索引（见 `EasilyNET.Mongo.Core` 文档），然后在应用启动时调用：

```csharp
var app = builder.Build();

// 自动扫描所有带索引特性的实体，后台创建/更新索引（不阻塞应用启动）
app.UseCreateMongoIndexes<MyDbContext>();
```

框架会：

1. 扫描 `MyDbContext` 的所有 `IMongoCollection<T>` 属性
2. 比对数据库中的现有索引与代码声明
3. 创建缺失的索引；对于结构变更的索引，优先尝试“先建后删”，冲突时回退为“删后建”

### 索引管理策略

默认情况下，框架只创建/更新代码中声明的索引，不会删除数据库中手动创建的索引（安全模式）。如需自动清理未在代码中声明的索引：

```csharp
builder.Services.AddMongoContext<MyDbContext>(builder.Configuration, c =>
{
    // 启用自动删除未管理的索引（谨慎！会删除 DBA 手动创建的索引）
    c.DropUnmanagedIndexes = true;

    // 保护特定前缀的索引不被删除（即使 DropUnmanagedIndexes = true）
    c.ProtectedIndexPrefixes.Add("dba_");      // 保护 DBA 手动创建的索引
    c.ProtectedIndexPrefixes.Add("analytics_"); // 保护分析用索引
});
```

> ⚠️ `DropUnmanagedIndexes` 在生产环境请谨慎使用。建议配合 `ProtectedIndexPrefixes` 保护重要索引。

> ⚠️ 时序集合（TimeSeries）上的时间字段由 MongoDB 自动索引，框架会自动跳过这些字段。

---

## 自动创建时序集合

**什么是时序集合？** 时序集合（Time Series Collection，MongoDB 5.0+）采用列式内部存储，针对时间递增数据（传感器、监控指标、行情数据）提供极高的压缩比和查询性能。

通过 `[TimeSeriesCollection]` 特性标记实体（详见 `EasilyNET.Mongo.Core` 文档）：

```csharp
[TimeSeriesCollection(
    collectionName: "sensor_readings",
    timeField: "timestamp",      // 时间字段（DateTime 类型）
    metaField: "deviceId",       // 分组字段（传感器ID、设备ID 等）
    granularity: TimeSeriesGranularity.Seconds)]
public class SensorReading
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string DeviceId { get; set; }
    public double Temperature { get; set; }
}
```

在 `Program.cs` 中启用自动创建：

```csharp
// 若集合不存在则自动创建，已存在则跳过
app.UseCreateMongoTimeSeriesCollection<MyDbContext>();
```

> ⚠️ 时序集合一旦创建，`timeField`/`metaField` 不可修改。`system.profile` 是保留名称不能使用。

---

## 自动创建固定大小集合

**什么是固定大小集合？** Capped Collection 类似环形缓冲区，存储达到上限后最老的文档自动被覆盖，天然维护“最近 N 条”语义，写入性能极高。适用于操作日志、审计记录、消息暂存等场景。

通过 `[CappedCollection]` 特性标记（详见 `EasilyNET.Mongo.Core` 文档）：

```csharp
// 保存最近 100MB 的操作日志
[CappedCollection(collectionName: "operation_logs", maxSize: 100 * 1024 * 1024)]
public class OperationLog
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Action { get; set; }
}

// 同时限制大小和条数（二者均满足才覆盖旧数据）
[CappedCollection("audit_logs", maxSize: 50 * 1024 * 1024, MaxDocuments = 50000)]
public class AuditLog
{
    // ...
}
```

```csharp
app.UseCreateMongoCappedCollections<MyDbContext>();
```

> ⚠️ Capped 集合不支持删除单个文档（只能 `drop` 整个集合）。

---

## Atlas Search / Vector Search 索引自动创建

Atlas Search 是基于 Apache Lucene 的全文搜索引擎，支持中文分词、相关性排序、自动补全。Vector Search 是 AI 语义搜索的核心能力，广泛用于 RAG（检索增强生成）场景。

通过 `[MongoSearchIndex]`、`[SearchField]`、`[VectorField]`、`[VectorFilterField]` 特性声明（详见 `EasilyNET.Mongo.Core` 文档）：

```csharp
[MongoSearchIndex(Name = "product_search")]
[MongoSearchIndex(Name = "product_vector", Type = ESearchIndexType.VectorSearch)]
public class Product
{
    public string Id { get; set; }

    // 中文全文搜索 + 自动补全
    [SearchField(ESearchFieldType.String, IndexName = "product_search",
        AnalyzerName = "lucene.chinese")]
    [SearchField(ESearchFieldType.Autocomplete, IndexName = "product_search",
        AnalyzerName = "lucene.chinese")]
    public string Name { get; set; }

    // 1536 维向量（对应 OpenAI text-embedding-ada-002）
    [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine,
        IndexName = "product_vector")]
    public float[] Embedding { get; set; }

    // 向量搜索的预过滤字段（只在指定分类中搜索）
    [VectorFilterField(IndexName = "product_vector")]
    public string Category { get; set; }
}
```

对于**未在 `MongoContext` 上声明为 `IMongoCollection<T>` 属性**的实体类型，可以通过 `CollectionName` 显式指定集合名称，框架会通过程序集扫描自动发现并创建索引：

```csharp
// 该实体不需要在 DbContext 中声明，框架通过程序集扫描自动发现
[MongoSearchIndex(Name = "log_search", CollectionName = "application_logs")]
public class ApplicationLog
{
    [SearchField(ESearchFieldType.String, AnalyzerName = "lucene.standard")]
    public string Message { get; set; }

    [SearchField(ESearchFieldType.Date)]
    public DateTime Timestamp { get; set; }
}
```

> 💡 如果实体已在 `MongoContext` 上声明为 `IMongoCollection<T>` 属性，则无需设置 `CollectionName`，集合名称会自动解析。

在启动时调用：

```csharp
// 方式 1（推荐）：在服务注册阶段添加后台服务，自动创建索引
builder.Services.AddMongoSearchIndexCreation<MyDbContext>();
```

```csharp
// 方式 2：在中间件管道中调用（异步后台创建，不阻塞应用启动）
app.UseCreateMongoSearchIndexes<MyDbContext>();
```

> 两种方式均需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。不支持的环境会记录警告并跳过，不影响应用启动。

在代码中执行向量搜索：

```csharp
// 先用 AI 模型生成查询向量，再进行语义搜索
var queryVector = await embeddingService.GetEmbeddingAsync("蓝牙耳机 降噪");

var pipeline = new BsonDocument[]
{
    new("$vectorSearch", new BsonDocument
    {
        { "index", "product_vector" },
        { "path", "embedding" },
        { "queryVector", new BsonArray(queryVector.Select(f => (BsonValue)f)) },
        { "numCandidates", 150 },
        { "limit", 10 },
        { "filter", new BsonDocument("category", "电子产品") }  // 预过滤
    })
};

var results = await db.Products.Aggregate<BsonDocument>(pipeline).ToListAsync();
```

---

## 变更流（Change Stream）

MongoDB 变更流是实时数据订阅机制，可监听集合的插入、更新、删除等操作。常用于：

- **跨系统数据同步**：将变更实时同步到 Elasticsearch、Redis 等
- **事件驱动架构**：数据变更触发下游业务（如发送通知、更新缓存）
- **审计追踪**：自动记录所有数据变更历史

> ⚠️ **要求**：变更流需要 MongoDB **副本集**（Replica Set）或 Atlas。

### 1. 定义变更流处理器

继承 `MongoChangeStreamHandler<TDocument>`，实现 `HandleChangeAsync`：

```csharp
using EasilyNET.Mongo.AspNetCore.ChangeStreams;
using EasilyNET.Mongo.AspNetCore.Options;

public class OrderChangeStreamHandler : MongoChangeStreamHandler<Order>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderChangeStreamHandler(
        IMongoDatabase database,
        ILogger<OrderChangeStreamHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(database, collectionName: "orders", logger, new ChangeStreamHandlerOptions
        {
            // 持久化恢复令牌：应用重启后从上次位置继续，不丢失事件
            PersistResumeToken = true,
            ResumeTokenCollectionName = "_changeStreamResumeTokens", // 令牌存储集合

            // 断线重连：指数退避策略
            MaxRetryAttempts = 5,                         // 最大重试次数（0 = 无限）
            RetryDelay = TimeSpan.FromSeconds(2),         // 初始间隔（2→4→8→16→32s）
            MaxRetryDelay = TimeSpan.FromSeconds(60),     // 最大间隔

            // FullDocument 策略（更新事件是否拉取完整文档）
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
        })
    {
        _scopeFactory = scopeFactory;
    }

    // 只监听插入和更新（不设置则监听全部操作类型）
    protected override ChangeStreamOperationType[]? WatchOperations =>
    [
        ChangeStreamOperationType.Insert,
        ChangeStreamOperationType.Update,
        ChangeStreamOperationType.Replace
    ];

    protected override async Task HandleChangeAsync(
        ChangeStreamDocument<Order> change,
        CancellationToken cancellationToken)
    {
        // 处理器本身是 Singleton，Scoped 服务需通过 Scope 获取
        using var scope = _scopeFactory.CreateScope();
        var notifier = scope.ServiceProvider.GetRequiredService<INotificationService>();

        switch (change.OperationType)
        {
            case ChangeStreamOperationType.Insert:
                await notifier.SendNewOrderAlertAsync(change.FullDocument, cancellationToken);
                break;

            case ChangeStreamOperationType.Update when change.FullDocument?.Status == "shipped":
                await notifier.SendShippedNotificationAsync(change.FullDocument, cancellationToken);
                break;
        }
    }
}
```

### 2. 注册处理器

```csharp
// 注册为后台服务，应用启动时自动开始监听
builder.Services.AddMongoChangeStreamHandler<OrderChangeStreamHandler>();
```

**`ChangeStreamHandlerOptions` 参数说明**：

| 属性                        | 默认值                        | 说明                                 |
| --------------------------- | ----------------------------- | ------------------------------------ |
| `MaxRetryAttempts`          | `5`                           | 断线后最大重试次数，`0` 表示无限重试 |
| `RetryDelay`                | `2s`                          | 首次重试间隔，后续每次翻倍           |
| `MaxRetryDelay`             | `60s`                         | 重试间隔上限                         |
| `PersistResumeToken`        | `false`                       | 是否将恢复令牌持久化到 MongoDB       |
| `ResumeTokenCollectionName` | `"_changeStreamResumeTokens"` | 存储恢复令牌的集合名                 |
| `FullDocument`              | `UpdateLookup`                | 更新事件是否返回完整文档             |

### 断点续传工作原理

```text
开启 PersistResumeToken = true 后：

事件 1 → HandleChangeAsync() → 保存 Token-A
事件 2 → HandleChangeAsync() → 保存 Token-B
[应用重启]
从 Token-B 恢复 → 继续处理事件 3、4、5 ...（无遗漏，无重复）
```

---

## GridFS 文件存储

GridFS 是 MongoDB 内置的大文件（> 16MB）分片存储方案，适合不引入额外对象存储的简单场景。

### 注册 GridFS

```csharp
// 使用默认数据库（集合：fs.files, fs.chunks）
builder.Services.AddGridFSBucket();

// 自定义桶名和块大小
builder.Services.AddGridFSBucket(opt =>
{
    opt.BucketName = "uploads";      // 集合前缀：uploads.files, uploads.chunks
    opt.ChunkSizeBytes = 512 * 1024;  // 每块 512KB（默认 255KB）
});

// 使用独立的数据库（文件库与业务库分离），通过键控服务注入
builder.Services.AddGridFSBucket(
    serviceKey: "media",              // DI 键名，注入时使用 [FromKeyedServices("media")]
    databaseName: "file-storage-db",  // 独立数据库名
    opt =>
    {
        opt.BucketName = "media";
    });
```

### 键控注入（多 GridFS 桶场景）

当注册了多个 GridFS 桶时，通过 `[FromKeyedServices]` 特性注入指定的桶：

```csharp
// 注册多个桶
builder.Services.AddGridFSBucket("media", "media-db");
builder.Services.AddGridFSBucket("documents", "docs-db");

// 在服务中注入
public class MediaService([FromKeyedServices("media")] IGridFSBucket mediaBucket)
{
    // mediaBucket 操作 media-db 数据库中的 fs.files / fs.chunks
}
```

### 使用 GridFS

```csharp
public class FileStorageService(IGridFSBucket gridFs)
{
    // 上传文件
    public async Task<string> UploadAsync(string fileName, Stream content, CancellationToken ct = default)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "contentType", "image/jpeg" },
                { "uploadedBy", "user_001" }
            }
        };
        var fileId = await gridFs.UploadFromStreamAsync(fileName, content, options, ct);
        return fileId.ToString();
    }

    // 下载文件（按 ID）
    public async Task<Stream> DownloadAsync(string fileId, CancellationToken ct = default)
    {
        var stream = new MemoryStream();
        await gridFs.DownloadToStreamAsync(ObjectId.Parse(fileId), stream, cancellationToken: ct);
        stream.Position = 0;
        return stream;
    }

    // 下载文件（按文件名，取最新版本）
    public async Task<Stream> DownloadByNameAsync(string fileName, CancellationToken ct = default)
    {
        var stream = new MemoryStream();
        await gridFs.DownloadToStreamByNameAsync(fileName, stream, cancellationToken: ct);
        stream.Position = 0;
        return stream;
    }

    // 删除文件
    public async Task DeleteAsync(string fileId, CancellationToken ct = default)
        => await gridFs.DeleteAsync(ObjectId.Parse(fileId), ct);

    // 查询文件信息
    public async Task<GridFSFileInfo?> FindInfoAsync(string fileId, CancellationToken ct = default)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq("_id", ObjectId.Parse(fileId));
        using var cursor = await gridFs.FindAsync(filter, cancellationToken: ct);
        return await cursor.FirstOrDefaultAsync(ct);
    }
}
```

---

## 健康检查

将 MongoDB 连通性纳入 ASP.NET Core 健康检查，与 Kubernetes 探针、负载均衡器集成：

```csharp
builder.Services.AddHealthChecks()
    .AddMongoHealthCheck(
        name: "mongodb",                        // 健康检查名称（默认 "mongodb"）
        failureStatus: HealthStatus.Unhealthy,   // 失败状态（默认 Unhealthy）
        tags: ["db", "mongodb"],              // 可选标签（用于分组过滤）
        timeout: TimeSpan.FromSeconds(5));       // 超时时间

var app = builder.Build();

// 暴露统一健康检查端点
app.MapHealthChecks("/health");

// Kubernetes：就绪探针（只检查 DB 连通性）
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("db")
});

// Kubernetes：存活探针（只检查进程存活）
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

健康检查通过向 MongoDB 发送 `ping` 命令验证连通性，超时或异常则返回 `Unhealthy`。

---

## 使用 EasilyNET.AutoDependencyInjection 集成

若使用模块化依赖注入体系，可将 MongoDB 配置封装为独立模块。

### 安装模块化 DI

```bash
dotnet add package EasilyNET.AutoDependencyInjection
```

### 创建 Mongo 模块

```csharp
using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;

public class MongoModule : AppModule
{
    public override async Task ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.ServiceProvider.GetConfiguration();

        context.Services.AddMongoContext<MyDbContext>(config, c =>
        {
            c.DefaultConventionRegistry = true;
            c.DatabaseName = "mydb";
        });

        // 序列化器
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());
        context.Services.RegisterDynamicSerializer();

        // GridFS
        context.Services.AddGridFSBucket();

        // 变更流处理器
        context.Services.AddMongoChangeStreamHandler<OrderChangeStreamHandler>();

        // Atlas Search / Vector Search 索引自动创建（推荐在服务注册阶段添加）
        context.Services.AddMongoSearchIndexCreation<MyDbContext>();

        // 健康检查
        context.Services.AddHealthChecks().AddMongoHealthCheck();

        await base.ConfigureServices(context);
    }
}
```

### 创建根模块

```csharp
[DependsOn(
    typeof(DependencyAppModule),
    typeof(MongoModule)
)]
public class AppWebModule : AppModule
{
    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        if (app is null) return;

        // 自动创建各类集合和索引
        app.UseCreateMongoIndexes<MyDbContext>();
        app.UseCreateMongoTimeSeriesCollection<MyDbContext>();
        app.UseCreateMongoCappedCollections<MyDbContext>();
        // 如果已在 ConfigureServices 中调用 AddMongoSearchIndexCreation，则无需再调用以下方法
        // app.UseCreateMongoSearchIndexes<MyDbContext>();

        app.UseAuthorization();
        await base.ApplicationInitialization(context);
    }
}
```

### Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddApplicationModules<AppWebModule>();

var app = builder.Build();
app.InitializeApplication();
app.MapControllers();
app.MapHealthChecks("/health");
app.Run();
```

---

## 常见问题排查

### MongoConnectionPoolPausedException: "The connection pool is in paused state"

连接池暂停意味着驱动将服务器标记为不可用。常见原因：

| 原因                    | 解决方式                                                       |
| ----------------------- | -------------------------------------------------------------- |
| 网络不可达 / 防火墙拦截 | 确认应用机器能访问 `host:port`，安全组已放行                   |
| 认证或 TLS 错误         | 检查用户名、密码、`authSource`、`tls` 参数                     |
| 单节点 / 代理访问       | 连接串加 `directConnection=true` 或 `loadBalanced=true`        |
| 副本集名称不匹配        | 连接串中的 `replicaSet` 必须与服务端一致                       |
| 连接池耗尽              | 降低 `MinConnectionPoolSize`，合理设置 `MaxConnectionPoolSize` |

推荐在连接串中显式设置超时：

```text
mongodb://user:pwd@host:27017/db?serverSelectionTimeoutMS=5000&connectTimeoutMS=5000&socketTimeoutMS=30000
```

### 变更流报错 "Change stream not supported on Standalone"

变更流需要副本集或 Atlas。本地开发可使用项目提供的 Docker Compose 启动：

```bash
docker compose -f docker-compose.mongo.rs.yml up -d
```

### Atlas Search 索引未创建

1. 确认使用的是 MongoDB Atlas 或 MongoDB 8.2+ 社区版
2. Atlas 侧索引创建是异步的，通常需要几秒到几分钟
3. 检查日志中是否有 `Failed to ensure search indexes` 错误
4. 确认已在服务注册阶段调用 `builder.Services.AddMongoSearchIndexCreation<MyDbContext>()`（推荐）
   或使用兼容方式 `app.UseCreateMongoSearchIndexes<MyDbContext>()`
