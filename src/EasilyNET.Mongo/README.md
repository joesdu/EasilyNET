## EasilyNET.Mongo

为 .NET 应用（ASP.NET Core / Worker Service / 控制台 / 通用 Host 均可）提供完整的 MongoDB 集成方案，涵盖连接注册、序列化、自动索引（含 Atlas Search / Vector Search）、变更流、GridFS
文件存储、固定集合/时序集合自动创建、健康检查等开箱即用能力。

> 📦 **包重命名**：本包由 `EasilyNET.Mongo.AspNetCore` 重命名而来，已不再依赖 ASP.NET Core 共享框架，可用于任意 .NET 宿主。迁移要点：
>
> 1. 包名 `EasilyNET.Mongo.AspNetCore` → `EasilyNET.Mongo`；
> 2. 命名空间 `EasilyNET.Mongo.AspNetCore.*` → `EasilyNET.Mongo.*`（注册扩展仍位于 `Microsoft.Extensions.DependencyInjection`，通常无需改 using）；
> 3. 集合/索引初始化由中间件 `app.UseCreateMongoXxx<T>()` 改为服务注册 `builder.Services.AddMongoXxxCreation<T>()`（详见下文各章节）。

---

## 目录

- [安装](#安装)
- [⚠️ 中断性变更（Breaking Changes）](#️-中断性变更breaking-changes)
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
dotnet add package EasilyNET.Mongo
```

---

## ⚠️ 中断性变更（Breaking Changes）

### 不再注册 `IMongoClient` 和 `IMongoDatabase` 到 DI 容器

此前版本会将 `IMongoClient` 和 `IMongoDatabase` 注册为单例服务，可直接通过 DI 注入。当前版本已移除此行为。

请通过 `MongoContext` 子类实例的 `Client` 和 `Database` 属性访问：

```csharp
// ❌ 旧方式（不再支持）
public class MyService(IMongoClient client, IMongoDatabase database) { }

// ✅ 新方式：通过 DbContext 获取
public class MyService(MyDbContext db)
{
    // 访问 IMongoClient
    var client = db.Client;

    // 访问 IMongoDatabase
    var database = db.Database;

    // 直接使用集合
    var orders = db.Orders;
}
```

### Convention 配置方式变更

Convention 相关配置已从 `ClientOptions` / `BasicClientOptions` 中移除，改为通过独立的 `ConfigureMongoConventions` 方法全局配置：

```csharp
// ❌ 旧方式（不再支持）
builder.Services.AddMongoContext<MyDbContext>(config, c =>
{
    c.DefaultConventionRegistry = true;
    c.ObjectIdToStringTypes = [typeof(SomeEntity)];
    c.ConventionRegistry = new() { ... };
});

// ✅ 新方式：Convention 独立配置，在 AddMongoContext 之前调用
builder.Services.ConfigureMongoConventions(c =>
{
    c.ObjectIdToStringTypes = [typeof(SomeEntity)];
    c.AddConvention("myConvention", new ConventionPack { ... });
});
builder.Services.AddMongoContext<MyDbContext>(config, c =>
{
    c.DatabaseName = "mydb";
});
```

> 若不调用 `ConfigureMongoConventions`，首次 `AddMongoContext` 时将自动使用默认配置（驼峰命名 + 忽略未知字段 + `_id`
> 映射 + 枚举存字符串）。

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

// 可选：自定义全局 Convention（必须在 AddMongoContext 之前调用，最多一次）
// 调用后仅使用用户自定义的约定，本库内置默认约定不会被应用
// 若不调用，首次 AddMongoContext 时自动使用默认配置
builder.Services.ConfigureMongoConventions(c =>
{
    // 特定类型的 ObjectId 存为 string（在使用 $unwind 时有时需要此配置）
    c.ObjectIdToStringTypes = [typeof(SomeEntity)];

    // 添加自定义约定包（支持链式调用）
    c.AddConvention("default", new ConventionPack
    {
        new CamelCaseElementNameConvention(),
        new IgnoreExtraElementsConvention(true),
        new NamedIdMemberConvention("Id", "ID"),
        new EnumRepresentationConvention(BsonType.String)
    })
    .AddConvention("ignoreDefault", new ConventionPack
    {
        new IgnoreIfDefaultConvention(true)
    });
});

builder.Services.AddMongoContext<MyDbContext>(builder.Configuration, c =>
{
    // 数据库名称（可选，覆盖连接字符串中的库名）
    c.DatabaseName = "mydb";

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
    c.DatabaseName = "mydb";
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
    });
```

### 弹性连接配置

MongoDB 驱动内置连接自动恢复，`Resilience` 提供开箱即用的默认超时与连接池配置，与驱动自带机制协同工作：

```csharp
builder.Services.AddMongoContext<MyDbContext>(builder.Configuration, c =>
{
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

| 场景          | 推荐配置                                                 |
|-------------|------------------------------------------------------|
| 同区域低延迟      | `ConnectTimeout=5s`, `ServerSelectionTimeout=5s`     |
| 跨区域高延迟      | `ConnectTimeout=20s`, `ServerSelectionTimeout=30s`   |
| 高并发大流量      | `MaxConnectionPoolSize=200+`, `WaitQueueTimeout=30s` |
| 代理 / 单节点    | 连接串加 `directConnection=true` 或 `loadBalanced=true`   |
| Atlas / 云托管 | 保持默认值，确保 `RetryReads=true`, `RetryWrites=true`       |

> ⚠️ 弹性配置不能解决网络/认证/拓扑错误，请先排查连接串配置。

---

## 字段映射与命名约定

默认情况下（未调用 `ConfigureMongoConventions`），框架会自动注册以下规则：

| 功能               | 说明                                            | 示例                       |
|------------------|-----------------------------------------------|--------------------------|
| **驼峰字段名**        | C# `PascalCase` → MongoDB `camelCase`         | `PageSize` → `pageSize`  |
| **`_id` 映射**     | 自动将 `_id` 与实体中的 `Id` / `ID` 属性互相映射            | `_id` ↔ `Id`             |
| **枚举存字符串**       | 枚举值以字符串形式存储，便于阅读                              | `Gender.Male` → `"Male"` |
| **忽略未知字段**       | 反序列化时忽略数据库中存在但代码中未定义的字段                       | 向前兼容                     |
| **DateTime 本地化** | `DateTime` 反序列化后自动设为 `DateTimeKind.Local`     | 时区一致                     |
| **Decimal128**   | `decimal` 类型自动映射为 MongoDB `Decimal128`，避免精度丢失 | 金额字段                     |

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

> ⚠️ `JsonNode` 反序列化不支持 Unicode 转义字符；若需要跨系统序列化，请将 `JsonSerializerOptions.Encoder` 设为
`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`。

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

通过 `[MongoIndex]` / `[MongoCompoundIndex]` 特性声明索引（见 `EasilyNET.Mongo.Core` 文档），然后在注册服务时调用：

```csharp
// 注册托管后台服务：应用启动后自动扫描所有带索引特性的实体，
// 使用异步驱动 API 创建/更新索引（不阻塞应用启动），完成后结束。
builder.Services.AddMongoIndexCreation<MyDbContext>();
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

在注册服务时启用自动创建（托管后台服务，启动后异步执行，不阻塞启动）：

```csharp
// 若集合不存在则自动创建，已存在则跳过
builder.Services.AddMongoTimeSeriesCollectionCreation<MyDbContext>();
```

> ⚠️ 时序集合一旦创建，`timeField`/`metaField` 不可修改。`system.profile` 是保留名称不能使用。

---

## 自动创建固定大小集合

**什么是固定大小集合？** Capped Collection 类似环形缓冲区，存储达到上限后最老的文档自动被覆盖，天然维护“最近 N
条”语义，写入性能极高。适用于操作日志、审计记录、消息暂存等场景。

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
builder.Services.AddMongoCappedCollectionCreation<MyDbContext>();
```

> ⚠️ Capped 集合不支持删除单个文档（只能 `drop` 整个集合）。

---

## Atlas Search / Vector Search 索引自动创建

Atlas Search 是基于 Apache Lucene 的全文搜索引擎，支持中文分词、相关性排序、自动补全。Vector Search 是 AI 语义搜索的核心能力，广泛用于
RAG（检索增强生成）场景。

通过 `[MongoSearchIndex]`、`[SearchField]`、`[VectorField]`、`[VectorFilterField]` 特性声明（详见 `EasilyNET.Mongo.Core`
文档）：

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

对于**未在 `MongoContext` 上声明为 `IMongoCollection<T>` 属性**的实体类型，可以通过 `CollectionName`
显式指定集合名称，框架会通过程序集扫描自动发现并创建索引：

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

在服务注册阶段添加后台服务（启动后异步创建，不阻塞应用启动）：

```csharp
builder.Services.AddMongoSearchIndexCreation<MyDbContext>();
```

> 需要 MongoDB Atlas 或 MongoDB 8.2+ 社区版。不支持的环境会记录警告并跳过，不影响应用启动。

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
using EasilyNET.Mongo.ChangeStreams;
using EasilyNET.Mongo.Options;

public class OrderChangeStreamHandler : MongoChangeStreamHandler<Order>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OrderChangeStreamHandler(
        MyDbContext db,
        ILogger<OrderChangeStreamHandler> logger,
        IServiceScopeFactory scopeFactory)
        : base(db.Database, collectionName: "orders", logger, new ChangeStreamHandlerOptions
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

| 属性                          | 默认值                           | 说明                   |
|-----------------------------|-------------------------------|----------------------|
| `MaxRetryAttempts`          | `5`                           | 断线后最大重试次数，`0` 表示无限重试 |
| `RetryDelay`                | `2s`                          | 首次重试间隔，后续每次翻倍        |
| `MaxRetryDelay`             | `60s`                         | 重试间隔上限               |
| `PersistResumeToken`        | `false`                       | 是否将恢复令牌持久化到 MongoDB  |
| `ResumeTokenCollectionName` | `"_changeStreamResumeTokens"` | 存储恢复令牌的集合名           |
| `FullDocument`              | `UpdateLookup`                | 更新事件是否返回完整文档         |

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
builder.Services.AddGridFSBucket<MyDbContext>();

// 自定义桶名和块大小
builder.Services.AddGridFSBucket<MyDbContext>(opt =>
{
    opt.BucketName = "uploads";      // 集合前缀：uploads.files, uploads.chunks
    opt.ChunkSizeBytes = 512 * 1024;  // 每块 512KB（默认 255KB）
});

// 使用独立的数据库（文件库与业务库分离），通过键控服务注入
builder.Services.AddGridFSBucket<MyDbContext>(
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
builder.Services.AddGridFSBucket<MyDbContext>("media", "media-db");
builder.Services.AddGridFSBucket<MyDbContext>("documents", "docs-db");

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
    .AddMongoHealthCheck<MyDbContext>(
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
using EasilyNET.Mongo.Serializers;
using EasilyNET.Mongo.ConsoleDebug.Subscribers;
using WebApi.Test.Unit.Common;

internal sealed class MongoModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Configuration;
        var env = context.Environment ?? throw new("获取环境信息出错");

        context.Services.AddMongoContext<DbContext>(config, c =>
        {
            c.DatabaseName = "easilynet";
            c.ClientSettings = cs =>
            {
                cs.ClusterConfigurator = s =>
                {
                    if (env.IsDevelopment())
                    {
                        s.Subscribe(new ActivityEventConsoleDebugSubscriber(new()
                        {
                            Enable = false
                        }));
                    }
                    s.Subscribe(new ActivityEventDiagnosticsSubscriber(new()
                    {
                        CaptureCommandText = true
                    }));
                };
                cs.ApplicationName = Constant.InstanceName;
            };
        });

        context.Services.AddMongoContext<DbContext2>(config, c =>
        {
            c.DatabaseName = "easilynet2";
            c.ClientSettings = cs =>
            {
                cs.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventConsoleDebugSubscriber(new()
                {
                    Enable = false
                }));
                cs.ApplicationName = Constant.InstanceName;
            };
        });

        // 序列化器
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());
        context.Services.RegisterSerializer(new JsonNodeSerializer());
        context.Services.RegisterSerializer(new JsonObjectSerializer());
        context.Services.RegisterDynamicSerializer();
        context.Services.RegisterGlobalEnumKeyDictionarySerializer();
        // 注册索引自动创建的后台服务（启动后异步执行，不阻塞启动）
        context.Services.AddMongoIndexCreation<DbContext>();
        context.Services.AddMongoIndexCreation<DbContext2>();
    }
}
```

### 创建根模块

```csharp
[DependsOn(
    typeof(DependencyAppModule),
    typeof(ResponseCompressionModule),
    typeof(CorsModule),
    typeof(ControllersModule),
    typeof(GarnetDistributedCacheModule),
    typeof(MongoModule)
    // ... 其他模块按中间件顺序继续追加
)]
internal sealed class AppWebModule : AppModule
{
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddProblemDetails();
        context.Services.AddExceptionHandler<BusinessExceptionHandler>();
        context.Services.AddHttpContextAccessor();
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseExceptionHandler();
        app?.UseResponseTime();
        app?.UseAuthentication();
        app?.UseAuthorization();
        app?.UseStaticFiles();
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

| 原因            | 解决方式                                                    |
|---------------|---------------------------------------------------------|
| 网络不可达 / 防火墙拦截 | 确认应用机器能访问 `host:port`，安全组已放行                            |
| 认证或 TLS 错误    | 检查用户名、密码、`authSource`、`tls` 参数                          |
| 单节点 / 代理访问    | 连接串加 `directConnection=true` 或 `loadBalanced=true`      |
| 副本集名称不匹配      | 连接串中的 `replicaSet` 必须与服务端一致                             |
| 连接池耗尽         | 降低 `MinConnectionPoolSize`，合理设置 `MaxConnectionPoolSize` |

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
4. 确认已在服务注册阶段调用 `builder.Services.AddMongoSearchIndexCreation<MyDbContext>()`

---

# EasilyNET MongoDB 套件 · 完整使用文档与场景选型指南

> 本节是对 `EasilyNET.Mongo`、`EasilyNET.Mongo.Core`、`EasilyNET.Mongo.ConsoleDebug` 三个包的**统一汇总**，
> 侧重「有哪些功能 / 什么场景该用哪个 / 怎么用」。如需某个功能的最细节说明，可对照各包自带的 `README.md`：
>
> - `EasilyNET.Mongo.Core/README.md` —— 上下文基类、特性、批量写入、聚合、地理空间
> - `EasilyNET.Mongo/README.md`（即本文件上半部分） —— 服务注册、序列化、自动建索引/集合、变更流、GridFS、健康检查
> - `EasilyNET.Mongo.ConsoleDebug/README.md` —— 命令行调试输出与 OpenTelemetry 诊断

---

## 目录（套件总览）

- [1. 套件结构与选型](#1-套件结构与选型)
- [2. 功能 → 场景速查表](#2-功能--场景速查表)
- [3. 最小可用配置（5 分钟上手）](#3-最小可用配置5-分钟上手)
- [4. 连接与上下文](#4-连接与上下文)
- [5. 序列化与命名约定](#5-序列化与命名约定)
- [6. 自动建索引 / 集合（启动后台服务）](#6-自动建索引--集合启动后台服务)
- [7. 查询增强：聚合、批量写入、地理空间](#7-查询增强聚合批量写入地理空间)
- [8. 实时数据：变更流（Change Stream）](#8-实时数据变更流change-stream)
- [9. 文件存储：GridFS](#9-文件存储gridfs)
- [10. 全文 / 向量搜索（Atlas Search）](#10-全文--向量搜索atlas-search)
- [11. 健康检查](#11-健康检查)
- [12. 调试与可观测性（ConsoleDebug）](#12-调试与可观测性consoledebug)
- [13. 部署前提与环境矩阵](#13-部署前提与环境矩阵)
- [14. 常见问题排查](#14-常见问题排查)

---

## 1. 套件结构与选型

| 包 | 角色 | 关键依赖 | 何时引用 |
|---|---|---|---|
| **EasilyNET.Mongo.Core** | 核心基础库：`MongoContext` 基类、全部特性、聚合/批量/地理空间扩展 | `MongoDB.Driver` | 仅写实体与查询逻辑、不需要 DI 注册时（如类库项目）。通常被 `EasilyNET.Mongo` 自动引用，无需单独安装。 |
| **EasilyNET.Mongo** | 宿主集成层：DI 注册、序列化、自动建索引/集合、变更流、GridFS、健康检查 | `Microsoft.Extensions.Hosting.Abstractions`、`HealthChecks`、`Mongo.Core` | 任意 .NET 宿主（ASP.NET Core / Worker / 控制台 / 通用 Host）需要开箱即用集成时。**绝大多数项目装这个即可。** |
| **EasilyNET.Mongo.ConsoleDebug** | 诊断层：命令输出到控制台 + OpenTelemetry 诊断 | `MongoDB.Driver`、`Spectre.Console.Json` | 开发期想看 MongoDB 实际执行命令，或接入 APM/链路追踪时按需引用。 |

- 目标框架：`net10.0`、`net11.0`。
- `EasilyNET.Mongo` 已不再依赖 ASP.NET Core 共享框架（由 `EasilyNET.Mongo.AspNetCore` 重命名而来），可用于任意 Host。

```bash
dotnet add package EasilyNET.Mongo              # 主包（含 Core）
dotnet add package EasilyNET.Mongo.ConsoleDebug # 可选，调试/诊断
```

---

## 2. 功能 → 场景速查表

| 你的需求 | 用什么 | 注册 / 标记入口 | 运行环境要求 |
|---|---|---|---|
| 连接数据库、集中管理集合 | 继承 `MongoContext` | `AddMongoContext<T>(...)` | 任意 |
| 生产环境超时/连接池/重试调优 | `Resilience` 选项 | `c.Resilience.Enable = true` | 任意 |
| 字段名驼峰、`_id↔Id`、枚举存字符串 | 内置约定（默认开启） | 不调用即默认；或 `ConfigureMongoConventions` | 任意 |
| `DateOnly`/`TimeOnly`/`JsonNode`/枚举键字典 持久化 | 自定义序列化器 | `RegisterSerializer(...)` 等 | 任意 |
| 启动时按特性自动建普通/复合/TTL/地理索引 | `[MongoIndex]` / `[MongoCompoundIndex]` | `AddMongoIndexCreation<T>()` | 任意 |
| 固定大小日志/环形缓冲 | `[CappedCollection]` | `AddMongoCappedCollectionCreation<T>()` | 任意 |
| 时序数据（IoT/监控/行情） | `[TimeSeriesCollection]` | `AddMongoTimeSeriesCollectionCreation<T>()` | MongoDB 5.0+ |
| 关联查询、分组统计、分桶、多维聚合 | 聚合扩展 | `LookupAndUnwindAsync` / `GroupByCountAsync` / `BucketAsync` / `FacetAsync` | 任意 |
| 一次往返完成混合增删改 | 批量写入 Fluent | `BulkWriteAsync(bulk => ...)` | 任意 |
| 附近搜索 / 区域筛选 / 含距离排序 | 地理空间扩展 | `NearSphere` / `GeoWithin` / `GeoNearAsync` | 需 `2dsphere` 索引 |
| 实时监听数据变更、跨系统同步、审计 | `MongoChangeStreamHandler<T>` | `AddMongoChangeStreamHandler<H>()` | **副本集 / Atlas** |
| 大文件存储（>16MB） | GridFS | `AddGridFSBucket<T>(...)` | 任意 |
| 中文全文搜索 / 自动补全 | `[MongoSearchIndex]` + `[SearchField]` | `AddMongoSearchIndexCreation<T>()` | **Atlas 或 MongoDB 8.2+** |
| AI 语义搜索 / RAG | `[MongoSearchIndex(VectorSearch)]` + `[VectorField]` | `AddMongoSearchIndexCreation<T>()` | **Atlas 或 MongoDB 8.2+** |
| K8s 探针 / 连通性监控 | 健康检查 | `AddMongoHealthCheck<T>()` | 任意 |
| 开发期查看执行命令 | ConsoleDebug 订阅器 | `ActivityEventConsoleDebugSubscriber` | 任意 |
| APM / 分布式链路追踪 | OpenTelemetry 订阅器 | `ActivityEventDiagnosticsSubscriber` | 任意 |

---

## 3. 最小可用配置（5 分钟上手）

```csharp
// 1) 定义上下文
public class AppDbContext : MongoContext
{
    public IMongoCollection<Order> Orders { get; set; } = default!;
    public IMongoCollection<User>  Users  { get; set; } = default!;
}

// 2) appsettings.json
//   { "ConnectionStrings": { "Mongo": "mongodb://localhost:27017/mydb" } }

// 3) 注册
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddMongoContext<AppDbContext>(builder.Configuration, c =>
{
    c.DatabaseName = "mydb";
});

// 4) 使用（构造函数注入 AppDbContext）
public class OrderService(AppDbContext db)
{
    public Task<List<Order>> AllAsync() => db.Orders.Find(_ => true).ToListAsync();
}
```

> ⚠️ **重要变更**：本套件**不再**向 DI 注册 `IMongoClient` / `IMongoDatabase`。请通过 `MongoContext` 子类的
> `db.Client` / `db.Database` 访问，避免多上下文歧义。

---

## 4. 连接与上下文

### 4.1 `MongoContext`（核心类型）

| 成员 | 说明 |
|---|---|
| `Client` | `IMongoClient` 实例 |
| `Database` | 连接串 / 选项中指定的数据库（默认库名 `easilynet`） |
| `GetCollection<T>(name)` | 运行时动态集合名场景（如按月分表 `logs_2026_01`） |
| `StartSessionAsync(startTransaction=false, ct)` | 开启会话/事务（**需副本集或 Atlas**） |

事务示例：

```csharp
using var session = await db.StartSessionAsync(startTransaction: true);
try
{
    await db.Accounts.UpdateOneAsync(session, fromFilter, decUpdate);
    await db.Accounts.UpdateOneAsync(session, toFilter,   incUpdate);
    await session.CommitTransactionAsync();
}
catch { await session.AbortTransactionAsync(); throw; }
```

### 4.2 三种注册方式

```csharp
// 方式 1：IConfiguration（推荐）—— 读取 ConnectionStrings:Mongo 或环境变量 CONNECTIONSTRINGS_MONGO
builder.Services.AddMongoContext<AppDbContext>(builder.Configuration, c => c.DatabaseName = "mydb");

// 方式 2：连接字符串
builder.Services.AddMongoContext<AppDbContext>("mongodb://localhost:27017/mydb", c => c.DatabaseName = "mydb");

// 方式 3：MongoClientSettings（高级：自定义凭据、集群配置）
builder.Services.AddMongoContext<AppDbContext>(
    new MongoClientSettings { Servers = [new("127.0.0.1", 27017)] },
    c => c.DatabaseName = "mydb");
```

### 4.3 弹性连接（`Resilience`）

生产建议显式开启，与驱动内置自动恢复协同工作（括号为默认值）：

```csharp
c.Resilience.Enable = true;
c.Resilience.ServerSelectionTimeout = TimeSpan.FromSeconds(10);
c.Resilience.ConnectTimeout         = TimeSpan.FromSeconds(10);
c.Resilience.SocketTimeout          = TimeSpan.FromSeconds(60);
c.Resilience.WaitQueueTimeout       = TimeSpan.FromMinutes(1);
c.Resilience.HeartbeatInterval      = TimeSpan.FromSeconds(10);
c.Resilience.MaxConnectionPoolSize  = 100;
c.Resilience.MinConnectionPoolSize  = null;   // null = 驱动默认
c.Resilience.RetryReads  = true;
c.Resilience.RetryWrites = true;
```

| 场景 | 推荐 |
|---|---|
| 同区域低延迟 | `ConnectTimeout=5s`，`ServerSelectionTimeout=5s` |
| 跨区域高延迟 | `ConnectTimeout=20s`，`ServerSelectionTimeout=30s` |
| 高并发大流量 | `MaxConnectionPoolSize=200+`，`WaitQueueTimeout=30s` |
| 单节点/代理 | 连接串加 `directConnection=true` 或 `loadBalanced=true` |

> 弹性配置无法解决网络/认证/拓扑错误，先排查连接串。

---

## 5. 序列化与命名约定

### 5.1 默认约定（未调用 `ConfigureMongoConventions` 时自动生效）

| 功能 | 示例 |
|---|---|
| 驼峰字段名 | `PageSize` → `pageSize` |
| `_id ↔ Id/ID` 映射 | `_id` ↔ `Id` |
| 枚举存字符串 | `Gender.Male` → `"Male"` |
| 忽略未知字段 | 向前兼容 |
| `DateTime` 本地化 | 反序列化设为 `DateTimeKind.Local` |
| `decimal` → `Decimal128` | 避免金额精度丢失 |

自定义全局约定（**必须在所有 `AddMongoContext` 之前、最多调用一次**；一旦调用则只用你声明的约定）：

```csharp
builder.Services.ConfigureMongoConventions(c =>
{
    c.DateTimeSerializerKind = DateTimeKind.Utc;        // 跨时区部署建议 Utc
    c.ObjectIdToStringTypes  = [typeof(SomeEntity)];    // 这些类型的 Id 存为 string（$unwind 场景常用）
    c.AddConvention("default", new ConventionPack
    {
        new CamelCaseElementNameConvention(),
        new IgnoreExtraElementsConvention(true),
        new NamedIdMemberConvention("Id", "ID"),
        new EnumRepresentationConvention(BsonType.String)
    });
});
```

### 5.2 自定义序列化器（在 `AddMongoContext` 之后注册）

```csharp
// DateOnly / TimeOnly —— 字符串可读，或 Ticks 省空间（二者同类型只能选其一）
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());     // 默认 "yyyy-MM-dd"
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());     // 默认 "HH:mm:ss.ffffff"
// builder.Services.RegisterSerializer(new DateOnlySerializerAsTicks());

// 动态类型 / 匿名对象（快速验证、原型）
builder.Services.RegisterDynamicSerializer();

// System.Text.Json 类型
builder.Services.RegisterSerializer(new JsonNodeSerializer());
builder.Services.RegisterSerializer(new JsonObjectSerializer());

// 枚举键字典 Dictionary<TEnum, TValue> 等
builder.Services.RegisterGlobalEnumKeyDictionarySerializer();
```

- `BsonDocumentJsonConverter`：在 Web API 中直接收发 `BsonDocument` 负载时，注册到 `JsonOptions.Converters`。

---

## 6. 自动建索引 / 集合（启动后台服务）

所有 `AddMongoXxxCreation<T>()` 均注册为**托管后台服务**：应用启动后**异步执行一次（不阻塞启动）**，完成即结束。

### 6.1 索引

```csharp
public class Order
{
    public string Id { get; set; }
    [MongoIndex(EIndexType.Ascending)]                       public string UserId { get; set; }
    [MongoIndex(EIndexType.Ascending, Unique = true)]        public string OrderNo { get; set; }
    [MongoIndex(EIndexType.Descending)]                      public DateTime CreatedAt { get; set; }
    [MongoIndex(EIndexType.Ascending, ExpireAfterSeconds = 2592000)] public DateTime ExpireAt { get; set; } // TTL
    [MongoIndex(EIndexType.Ascending, Sparse = true)]        public string? ExternalId { get; set; }
}

// 复合索引（类级）：遵循 ESR —— 等值→排序→范围
[MongoCompoundIndex(["userId", "createdAt"], [EIndexType.Ascending, EIndexType.Descending], Name = "idx_user_time")]
public class Order { /* ... */ }

builder.Services.AddMongoIndexCreation<AppDbContext>();
```

`EIndexType`：`Ascending` / `Descending` / `Geo2D` / `Geo2DSphere`(推荐地理) / `Hashed`(分片键) / `Text` / `Multikey`(数组自动) / `Wildcard`(动态字段)。

**索引清理策略**（默认安全模式，只新增/更新、不删除手工索引）：

```csharp
c.DropUnmanagedIndexes = true;            // ⚠️ 谨慎：会删除代码外的索引
c.ProtectedIndexPrefixes.Add("dba_");     // 即便开启清理，这些前缀也保护不删
```

### 6.2 固定大小集合（Capped）—— 日志、审计、环形缓冲

```csharp
[CappedCollection("operation_logs", maxSize: 100 * 1024 * 1024)]          // 100MB 上限
[CappedCollection("audit_logs", maxSize: 50 * 1024 * 1024, MaxDocuments = 50000)] // 同时限大小+条数
public class OperationLog { /* ... */ }

builder.Services.AddMongoCappedCollectionCreation<AppDbContext>();
```

> 不支持删除单文档（只能 `drop` 整个集合）；`MaxDocuments` 必须配合 `MaxSize`。

### 6.3 时序集合（Time Series）—— IoT / 监控 / 行情

```csharp
[TimeSeriesCollection("sensor_readings", timeField: "timestamp", metaField: "deviceId",
    granularity: TimeSeriesGranularity.Seconds, ExpireAfter = 86400 * 30)] // 可选 30 天 TTL
public class SensorReading
{
    public DateTime Timestamp { get; set; }  // timeField
    public string DeviceId { get; set; }     // metaField
    public double Temperature { get; set; }
}

builder.Services.AddMongoTimeSeriesCollectionCreation<AppDbContext>();
```

> 需 MongoDB 5.0+；`timeField`/`metaField` 创建后不可改；`system.profile` 为保留名。框架会自动为 `(metaField, timeField)` 建复合索引，并跳过时序时间字段的常规索引。

---

## 7. 查询增强：聚合、批量写入、地理空间

> 这些是 `EasilyNET.Mongo.Core` 提供的 `IMongoCollection<T>` 扩展，任意环境可用。

### 7.1 聚合扩展

```csharp
// 关联查询（LEFT JOIN）：$lookup + $unwind
var rows = await db.Orders.LookupAndUnwindAsync<User>(
    foreignCollectionName: "users", localField: o => o.UserId, foreignField: u => u.Id,
    asField: "user", preserveNullAndEmpty: true); // false = INNER JOIN

// 分组计数：GROUP BY status
var counts = await db.Orders.GroupByCountAsync(o => o.Status);

// 区间分布
var dist = await db.Orders.BucketAsync(o => o.Amount,
    boundaries: [0, 100, 500, 1000, 5000, (BsonValue)BsonMaxKey.Value], defaultBucket: "其它");

// 多维并行聚合（电商多面统计）
var facet = await db.Products.FacetAsync(new() { ["byBrand"] = [ /* stages */ ] });
```

### 7.2 批量写入 Fluent —— 迁移、批量导入、原子混合操作

```csharp
var r = await db.Orders.BulkWriteAsync(bulk => bulk
    .InsertOne(new Order { /* ... */ })
    .UpdateOne(filter, Builders<Order>.Update.Set(o => o.Status, "shipped"))
    .UpdateMany(staleFilter, timeoutUpdate)
    .ReplaceOne(idFilter, replacement, isUpsert: true)
    .DeleteMany(Builders<Order>.Filter.Lt(o => o.CreatedAt, DateTime.UtcNow.AddYears(-3))));
// r.InsertedCount / r.ModifiedCount / r.DeletedCount
```

### 7.3 地理空间 —— 附近门店、区域筛选

```csharp
public class Store
{
    [MongoIndex(EIndexType.Geo2DSphere)]   // 前提：2dsphere 索引
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
}

// 5 公里内（注意经度在前、纬度在后）
var near = GeoQueryExtensions.NearSphere<Store>(s => s.Location, 121.4737, 31.2304, maxDistanceMeters: 5000);
var list = await db.Stores.Find(near).ToListAsync();

// 含距离排序（结果附加 distance 字段，单位米）
var withDist = await db.Stores.GeoNearAsync(s => s.Location, GeoPoint.From(121.4737, 31.2304),
    new GeoNearOptions { MaxDistanceMeters = 10_000, Limit = 20, DistanceField = "distance" });
```

工厂：`GeoPoint.From(lon, lat)`、`GeoPolygon.From((lon,lat), ... , 闭合点)`。
过滤器：`NearSphere`（按距离）、`GeoWithin`（区域内，更快但无距离）、`GeoIntersects`（相交）。

---

## 8. 实时数据：变更流（Change Stream）

> **要求：副本集或 Atlas**（单节点不支持）。用于跨系统同步、事件驱动、审计追踪。

```csharp
public class OrderChangeStreamHandler : MongoChangeStreamHandler<Order>
{
    public OrderChangeStreamHandler(AppDbContext db, ILogger<OrderChangeStreamHandler> logger)
        : base(db.Database, collectionName: "orders", logger, new ChangeStreamHandlerOptions
        {
            PersistResumeToken = true,                 // 重启后断点续传，不丢事件
            MaxRetryAttempts = 5,                      // 0 = 无限重试
            RetryDelay = TimeSpan.FromSeconds(2),      // 指数退避 2→4→8…
            MaxRetryDelay = TimeSpan.FromSeconds(60),
            FullDocument = ChangeStreamFullDocumentOption.UpdateLookup,
        }) { }

    protected override ChangeStreamOperationType[]? WatchOperations =>
        [ChangeStreamOperationType.Insert, ChangeStreamOperationType.Update];

    protected override Task HandleChangeAsync(ChangeStreamDocument<Order> change, CancellationToken ct)
    {
        // 处理器为 Singleton，Scoped 服务需用 IServiceScopeFactory.CreateScope()
        return Task.CompletedTask;
    }
}

builder.Services.AddMongoChangeStreamHandler<OrderChangeStreamHandler>();
```

| 选项 | 默认 | 说明 |
|---|---|---|
| `MaxRetryAttempts` | 5 | 0=无限 |
| `RetryDelay` / `MaxRetryDelay` | 2s / 60s | 指数退避 |
| `PersistResumeToken` | false | 持久化恢复令牌（重启续传） |
| `ResumeTokenCollectionName` | `_changeStreamResumeTokens` | 令牌集合名 |
| `FullDocument` | `UpdateLookup` | 更新事件是否带完整文档 |

---

## 9. 文件存储：GridFS

适合不想引入额外对象存储、又要存大文件（>16MB）的场景。

```csharp
// 默认库（fs.files / fs.chunks）
builder.Services.AddGridFSBucket<AppDbContext>();

// 自定义桶名 / 块大小
builder.Services.AddGridFSBucket<AppDbContext>(o => { o.BucketName = "uploads"; o.ChunkSizeBytes = 512 * 1024; });

// 独立库 + 键控注入（多桶）
builder.Services.AddGridFSBucket<AppDbContext>("media", "file-db", o => o.BucketName = "media");
// 注入：MediaService([FromKeyedServices("media")] IGridFSBucket bucket)
```

常用操作：`UploadFromStreamAsync` / `DownloadToStreamAsync` / `DownloadToStreamByNameAsync` / `DeleteAsync` / `FindAsync`。

---

## 10. 全文 / 向量搜索（Atlas Search）

> **要求：MongoDB Atlas 或 MongoDB 8.2+ 社区版**。不支持的环境会记录警告并跳过，不影响启动。

```csharp
[MongoSearchIndex(Name = "product_search")]
[MongoSearchIndex(Name = "product_vector", Type = ESearchIndexType.VectorSearch)]
public class Product
{
    [SearchField(ESearchFieldType.String, IndexName = "product_search", AnalyzerName = "lucene.chinese")]
    [SearchField(ESearchFieldType.Autocomplete, IndexName = "product_search", AnalyzerName = "lucene.chinese")]
    public string Name { get; set; }

    [SearchField(ESearchFieldType.Number, IndexName = "product_search")]
    public decimal Price { get; set; }

    // AI 嵌入向量（OpenAI text-embedding-ada-002 = 1536 维）
    [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine, IndexName = "product_vector")]
    public float[] Embedding { get; set; }

    // 向量搜索预过滤字段（先缩小范围再算相似度）
    [VectorFilterField(IndexName = "product_vector")]
    public string Category { get; set; }
}

builder.Services.AddMongoSearchIndexCreation<AppDbContext>();
```

- `ESearchFieldType`：`String`/`Autocomplete`/`Number`/`Date`/`Boolean`/`ObjectId`/`Geo`/`Token`(精确)/`Document`。
- `EVectorSimilarity`：`Cosine`(推荐，归一化文本嵌入) / `DotProduct` / `Euclidean`。
- 实体若**未**在 `MongoContext` 上声明集合属性，需在 `[MongoSearchIndex]` 上设 `CollectionName`，框架通过程序集扫描发现。

向量检索（`$vectorSearch`）：

```csharp
var qv = await embedding.GetEmbeddingAsync("蓝牙耳机 降噪");
var pipeline = new BsonDocument[]
{
    new("$vectorSearch", new BsonDocument
    {
        { "index", "product_vector" }, { "path", "embedding" },
        { "queryVector", new BsonArray(qv.Select(f => (BsonValue)f)) },
        { "numCandidates", 150 }, { "limit", 10 },
        { "filter", new BsonDocument("category", "电子产品") }   // 预过滤
    })
};
var hits = await db.Products.Aggregate<BsonDocument>(pipeline).ToListAsync();
```

---

## 11. 健康检查

```csharp
builder.Services.AddHealthChecks()
    .AddMongoHealthCheck<AppDbContext>(
        name: "mongodb", failureStatus: HealthStatus.Unhealthy,
        tags: ["db", "mongodb"], timeout: TimeSpan.FromSeconds(5));

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("db") }); // K8s 就绪
app.MapHealthChecks("/health/live",  new() { Predicate = _ => false });                  // K8s 存活
```

通过对数据库发送 `ping` 命令验证连通性。

---

## 12. 调试与可观测性（ConsoleDebug）

通过 `ClientSettings` 在 `ClusterConfigurator` 上挂订阅器：

```csharp
builder.Services.AddMongoContext<AppDbContext>(builder.Configuration, c =>
{
    c.ClientSettings = cs => cs.ClusterConfigurator = cb =>
    {
        // 开发期：命令以面板形式输出到控制台
        if (builder.Environment.IsDevelopment())
            cb.Subscribe(new ActivityEventConsoleDebugSubscriber(new() { Enable = true }));

        // 任意环境：OpenTelemetry 诊断（接 APM / Jaeger / Tempo）
        cb.Subscribe(new ActivityEventDiagnosticsSubscriber(new()
        {
            CaptureCommandText = true,     // 捕获命令文本
            ExcludeGridFSChunks = true,    // 默认开启，避免 chunks 二进制撑爆内存
            MaxCommandTextLength = 1000    // 超长截断（0 = 不限）
        }));
    };
});
```

- `ConsoleDebugInstrumentationOptions`：`Enable`、`ShouldStartCollection`（按集合名过滤输出）。
- `ActivityEventDiagnosticsSubscriber` 按 OpenTelemetry 语义约定打 `db.system`、`db.namespace`、`db.operation.name` 等标签。
- ⚠️ GridFS 场景务必保留 `ExcludeGridFSChunks = true`，否则会捕获大量二进制数据。

---

## 13. 部署前提与环境矩阵

| 功能 | 单节点 Standalone | 副本集 / 分片 | Atlas | 备注 |
|---|:---:|:---:|:---:|---|
| 基本读写 / 索引 / Capped / GridFS | ✅ | ✅ | ✅ | — |
| 多文档事务 | ❌ | ✅ | ✅ | `StartSessionAsync(true)` |
| 变更流 | ❌ | ✅ | ✅ | — |
| 时序集合 | ✅ | ✅ | ✅ | MongoDB 5.0+ |
| Atlas Search / Vector Search | ⚠️ 仅 8.2+ | ⚠️ 仅 8.2+ | ✅ | 社区版需 8.2+ |

> 本地开发副本集（变更流/事务）可用项目提供的 `docker compose -f docker-compose.mongo.rs.yml up -d`。

---

## 14. 常见问题排查

| 现象 | 可能原因 / 解决 |
|---|---|
| `MongoConnectionPoolPausedException`（连接池暂停） | 网络/防火墙、认证/TLS、单节点未加 `directConnection=true`、副本集名不匹配、连接池耗尽。连接串显式设 `serverSelectionTimeoutMS`、`connectTimeoutMS`、`socketTimeoutMS`。 |
| `Change stream not supported on Standalone` | 变更流需副本集/Atlas，启动本地副本集。 |
| Atlas Search 索引未生成 | 确认 Atlas 或 8.2+；索引创建异步需等待；查日志 `Failed to ensure search indexes`；确认已调用 `AddMongoSearchIndexCreation<T>()`。 |
| 注入 `IMongoClient`/`IMongoDatabase` 报找不到 | 已不再注册，改用 `db.Client` / `db.Database`。 |
| `ConfigureMongoConventions` 不生效 | 必须在所有 `AddMongoContext` **之前**调用，且最多一次。 |
| 同类型 `DateOnly` 序列化器冲突 | String 与 Ticks 同类型只能注册其一。 |
| 跨时区读到的时间不一致 | `ConfigureMongoConventions` 中设 `DateTimeSerializerKind = DateTimeKind.Utc`。 |
