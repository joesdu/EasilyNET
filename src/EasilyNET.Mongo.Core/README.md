## EasilyNET.Mongo.Core

`EasilyNET.Mongo.Core` 是 MongoDB 集成体系的**核心基础库**，提供 `MongoContext` 基类、所有标注特性（Attribute）、数据类型辅助类、批量写入 Fluent API、聚合管道扩展以及地理空间查询工具。业务层和 AspNetCore 集成层均依赖此包。

---

## 目录

- [安装](#安装)
- [MongoContext —— 数据库上下文基类](#mongocontext--数据库上下文基类)
- [索引特性](#索引特性)
  - [MongoIndexAttribute —— 单字段索引](#mongoindexattribute--单字段索引)
  - [MongoCompoundIndexAttribute —— 复合索引](#mongocompoundindexattribute--复合索引)
- [集合类型特性](#集合类型特性)
  - [TimeSeriesCollectionAttribute —— 时序集合](#timeseriescollectionattribute--时序集合)
  - [CappedCollectionAttribute —— 固定大小集合](#cappedcollectionattribute--固定大小集合)
- [Atlas Search 特性（云原生/AI 场景）](#atlas-search-特性云原生ai-场景)
  - [MongoSearchIndexAttribute —— 搜索索引](#mongosearchindexattribute--搜索索引)
  - [SearchFieldAttribute —— 搜索字段](#searchfieldattribute--搜索字段)
  - [VectorFieldAttribute —— 向量字段](#vectorfieldattribute--向量字段)
  - [VectorFilterFieldAttribute —— 向量过滤字段](#vectorfilterfieldattribute--向量过滤字段)
- [批量写入 Fluent API](#批量写入-fluent-api)
- [聚合管道扩展](#聚合管道扩展)
- [地理空间查询](#地理空间查询)
- [Unwind 辅助类型](#unwind-辅助类型)

---

## 安装

```bash
dotnet add package EasilyNET.Mongo.Core
```

> 通常你不需要直接安装此包，因为 `EasilyNET.Mongo.AspNetCore` 已自动引用它。

---

## MongoContext —— 数据库上下文基类

`MongoContext` 是所有自定义数据库上下文的基类，类似于 EF Core 中的 `DbContext`。它封装了 `IMongoClient` 和 `IMongoDatabase`，并提供事务会话支持。

### 什么是 MongoContext？

在实际项目中，你需要继承 `MongoContext` 来定义自己的集合属性，集中管理数据库访问入口：

```csharp
public class MyDbContext : MongoContext
{
    // 声明集合属性，属性名即集合名（受命名约定影响）
    public IMongoCollection<Order> Orders { get; set; } = null!;
    public IMongoCollection<User> Users { get; set; } = null!;
    public IMongoCollection<Product> Products { get; set; } = null!;
}
```

然后通过依赖注入在任意服务中使用：

```csharp
public class OrderService(MyDbContext db)
{
    // 查询全部订单
    public async Task<List<Order>> GetAllAsync()
        => await db.Orders.Find(_ => true).ToListAsync();

    // 按条件查询
    public async Task<Order?> GetByIdAsync(string id)
        => await db.Orders.Find(o => o.Id == id).FirstOrDefaultAsync();
}
```

### 事务支持

MongoDB 4.0+ 副本集 / Atlas 支持多文档事务。`MongoContext` 提供了便捷的会话入口：

```csharp
public class TransferService(MyDbContext db)
{
    public async Task TransferAsync(string fromId, string toId, decimal amount)
    {
        // 开启带事务的会话
        using var session = await db.StartSessionAsync(startTransaction: true);
        try
        {
            // 所有操作都在同一 session 下执行，保证原子性
            await db.Accounts.UpdateOneAsync(session,
                Builders<Account>.Filter.Eq(a => a.Id, fromId),
                Builders<Account>.Update.Inc(a => a.Balance, -amount));

            await db.Accounts.UpdateOneAsync(session,
                Builders<Account>.Filter.Eq(a => a.Id, toId),
                Builders<Account>.Update.Inc(a => a.Balance, amount));

            await session.CommitTransactionAsync();
        }
        catch
        {
            await session.AbortTransactionAsync();
            throw;
        }
    }
}
```

> ⚠️ **注意**：MongoDB 的事务要求副本集或 Atlas 集群，单节点（Standalone）不支持事务。

### 动态获取集合

当集合名称是运行时动态决定时，可使用 `GetCollection<T>()` 方法：

```csharp
var collection = db.GetCollection<LogEntry>("logs_2026_01");
```

---

## 索引特性

### MongoIndexAttribute —— 单字段索引

**作用**：标记单个属性，告诉框架在该字段上自动创建索引。应用启动时由 `UseCreateMongoIndexes<T>()` 自动执行。

**适用场景**：

- 查询条件中频繁出现的字段（如 `UserId`、`Status`、`CreatedAt`）
- 需要唯一约束的字段（如 `Email`、`Phone`）
- 需要 TTL 自动过期的字段（如日志、验证码）

```csharp
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;

public class Order
{
    public string Id { get; set; }

    // 普通升序索引 —— 加速按用户查询
    [MongoIndex(EIndexType.Ascending)]
    public string UserId { get; set; }

    // 唯一索引 —— 保证订单号不重复
    [MongoIndex(EIndexType.Ascending, Unique = true, Name = "idx_order_no")]
    public string OrderNo { get; set; }

    // 降序索引 —— 加速 "最新订单" 类排序
    [MongoIndex(EIndexType.Descending)]
    public DateTime CreatedAt { get; set; }

    // TTL 索引 —— 30天后自动删除文档（适合验证码、Token 等）
    [MongoIndex(EIndexType.Ascending, ExpireAfterSeconds = 2592000)]
    public DateTime ExpireAt { get; set; }

    // 文本索引 —— 支持全文检索（MongoDB 内置文本搜索，非 Atlas Search）
    [MongoIndex(EIndexType.Text)]
    public string Description { get; set; }

    // 稀疏索引 —— 仅索引字段存在的文档，节省空间
    [MongoIndex(EIndexType.Ascending, Sparse = true)]
    public string? ExternalId { get; set; }

    public decimal Amount { get; set; }
    public string Status { get; set; }
}
```

**支持的索引类型（`EIndexType`）**：

| 枚举值        | 说明                          |
| ------------- | ----------------------------- |
| `Ascending`   | 升序索引，最常用              |
| `Descending`  | 降序索引，加速逆序排序        |
| `Geo2D`       | 平面坐标索引（旧式，不推荐）  |
| `Geo2DSphere` | 球面地理索引（推荐，GeoJSON） |
| `Hashed`      | 哈希索引，用于分片键          |
| `Text`        | 全文文本索引                  |
| `Multikey`    | 多键索引，数组字段自动创建    |
| `Wildcard`    | 通配符索引，动态字段场景      |

### MongoCompoundIndexAttribute —— 复合索引

**作用**：在类级别标记，定义跨多个字段的复合索引。复合索引遵循"最左前缀"原则，比多个单字段索引更高效。

**适用场景**：

- 查询同时过滤多个字段：`WHERE UserId = ? AND Status = ?`
- 覆盖索引（Index Covering）：查询的所有字段都在索引中
- 排序 + 过滤组合：`WHERE UserId = ? ORDER BY CreatedAt DESC`

```csharp
using EasilyNET.Mongo.Core.Attributes;

// 复合索引：先按 UserId 升序，再按 CreatedAt 降序
// 能高效支持 "查询某用户的最新订单" 类场景
[MongoCompoundIndex(["userId", "createdAt"],
    [EIndexType.Ascending, EIndexType.Descending],
    Name = "idx_user_time")]

// 覆盖索引：查询覆盖 userId + status + amount，无需回表
[MongoCompoundIndex(["userId", "status", "amount"],
    [EIndexType.Ascending, EIndexType.Ascending, EIndexType.Ascending],
    Name = "idx_user_status_amount")]
public class Order
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string Status { get; set; }
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

> 💡 **MongoDB 索引设计原则**：
>
> - 优先使用复合索引替代多个单字段索引
> - 遵循 ESR 原则：等值（Equality）→ 排序（Sort）→ 范围（Range）
> - 不要无限叠加索引，每个索引都会影响写入性能

---

## 集合类型特性

### TimeSeriesCollectionAttribute —— 时序集合

**什么是时序集合？**

时序集合（Time Series Collection）是 MongoDB 5.0+ 引入的专为时间序列数据优化的集合类型。它内部采用列式存储，对时间递增的数据（如传感器、监控指标、日志流）提供极高的压缩比和查询性能。

**适用场景**：

- IoT 设备传感器数据（温度、湿度、GPS 轨迹）
- 监控系统指标（CPU 使用率、内存、QPS）
- 金融行情数据（股票价格、订单簿）
- 用户行为埋点数据

```csharp
using EasilyNET.Mongo.Core.Attributes;
using MongoDB.Driver;

// 基本用法：按秒粒度的传感器数据
[TimeSeriesCollection(
    collectionName: "sensor_readings",  // 集合名
    timeField: "timestamp",             // 时间字段（必须！类型应为 DateTime）
    metaField: "deviceId",              // 元数据字段，用于分组（如设备ID、传感器ID）
    granularity: TimeSeriesGranularity.Seconds,  // 粒度：Seconds/Minutes/Hours
    ExpireAfter = 86400 * 30)]          // 可选：30天后自动删除（单位：秒）
public class SensorReading
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }  // timeField
    public string DeviceId { get; set; }     // metaField
    public double Temperature { get; set; }
    public double Humidity { get; set; }
}
```

```csharp
// 高级用法：自定义桶配置（MongoDB 6.3+）
[TimeSeriesCollection(
    collectionName: "stock_ticks",
    timeField: "tradeTime",
    metaField: "symbol",
    bucketMaxSpanSeconds: 3600,         // 每个存储桶最大跨越 1 小时
    bucketRoundingSeconds: 3600)]       // 桶边界对齐到整点
public class StockTick
{
    public DateTime TradeTime { get; set; }
    public string Symbol { get; set; }
    public decimal Price { get; set; }
    public long Volume { get; set; }
}
```

在 `Program.cs` 中启用自动创建：

```csharp
// 在 app.UseXxx() 之后调用
app.UseCreateMongoTimeSeriesCollection<MyDbContext>();
```

> ⚠️ **注意**：
>
> - 时序集合一旦创建，`timeField`/`metaField` 不可修改
> - 不能对时序集合执行 `$out`（会绕过时序优化）
> - `system.profile` 是保留名称，不能作为集合名

### CappedCollectionAttribute —— 固定大小集合

**什么是固定大小集合？**

固定大小集合（Capped Collection）是一种循环覆盖的集合，类似环形缓冲区。当存储达到上限时，最老的文档会被自动覆盖，天然维护"最近 N 条"语义，且写入性能极高（顺序写）。

**适用场景**：

- 操作日志、审计日志（只保留最近 10 万条）
- 消息队列的临时暂存
- 系统事件流（最近 N MB 的事件）
- 实时聊天记录（只保留最近 N 条）

```csharp
using EasilyNET.Mongo.Core.Attributes;

// 保存最近 100MB 的日志，最多 100000 条
[CappedCollection(
    collectionName: "operation_logs",
    maxSize: 100 * 1024 * 1024)]        // maxSize 单位：字节，必须 > 0
public class OperationLog
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserId { get; set; }
    public string Action { get; set; }
    public string Details { get; set; }
}

// 同时限制大小和数量
[CappedCollection("audit_logs", maxSize: 50 * 1024 * 1024, MaxDocuments = 50000)]
public class AuditLog
{
    public string Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Resource { get; set; }
    public string Operation { get; set; }
}
```

在 `Program.cs` 中启用自动创建：

```csharp
app.UseCreateMongoCappedCollections<MyDbContext>();
```

> ⚠️ **注意**：
>
> - Capped 集合不支持删除单个文档（只能 `drop` 整个集合）
> - 大小和数量**同时满足**才能触发覆盖（即两者同时约束）
> - `MaxDocuments` 不独立限制，必须配合 `MaxSize` 使用

---

## Atlas Search 特性（云原生/AI 场景）

Atlas Search 是 MongoDB Atlas 提供的基于 Apache Lucene 的全文搜索引擎，支持中文分词、相关性排序、自动补全等高级功能。Vector Search 则是面向 AI/ML 的向量相似度搜索，是构建 RAG（检索增强生成）等 AI 应用的核心能力。

> ⚠️ **前提**：Atlas Search 和 Vector Search 需要 **MongoDB Atlas** 或 **MongoDB 8.2+ 社区版**。自托管的低版本 MongoDB 不支持。

### MongoSearchIndexAttribute —— 搜索索引

标记在类上，声明该集合需要哪些 Search 或 Vector Search 索引。支持同时创建多个索引。

```csharp
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;

// 普通全文搜索索引
[MongoSearchIndex(Name = "default")]

// 向量搜索索引（用于 AI 语义搜索）
[MongoSearchIndex(Name = "vector_index", Type = ESearchIndexType.VectorSearch)]

// 动态映射：所有字段自动加入搜索索引（无需逐一标注字段）
[MongoSearchIndex(Name = "dynamic_search", Dynamic = true)]
public class Article
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Category { get; set; }

    // 1536 维向量，对应 OpenAI text-embedding-ada-002
    public float[] Embedding { get; set; }
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

应用启动时自动创建（异步，不阻塞启动）：

```csharp
app.UseCreateMongoSearchIndexes<MyDbContext>();
```

### SearchFieldAttribute —— 搜索字段

与 `[MongoSearchIndex]` 配合，精细控制字段级别的索引映射。同一字段可标注多个不同类型。

```csharp
[MongoSearchIndex(Name = "product_search")]
public class Product
{
    public string Id { get; set; }

    // 中文字符串搜索，使用中文分析器
    [SearchField(ESearchFieldType.String, IndexName = "product_search",
        AnalyzerName = "lucene.chinese")]
    // 同时支持自动补全（用户输入"蓝牙"时匹配"蓝牙耳机"）
    [SearchField(ESearchFieldType.Autocomplete, IndexName = "product_search",
        AnalyzerName = "lucene.chinese", MinGrams = 1, MaxGrams = 10)]
    public string Name { get; set; }

    // 普通字符串，标准分析器
    [SearchField(ESearchFieldType.String, IndexName = "product_search")]
    public string Description { get; set; }

    // 数字字段，支持范围查询：价格 100~500 元
    [SearchField(ESearchFieldType.Number, IndexName = "product_search")]
    public decimal Price { get; set; }

    // 日期字段，支持时间范围搜索
    [SearchField(ESearchFieldType.Date, IndexName = "product_search")]
    public DateTime? OnSaleDate { get; set; }

    // Token 字段：SKU 精确匹配（无分词，完整字符串匹配）
    [SearchField(ESearchFieldType.Token, IndexName = "product_search")]
    public string Sku { get; set; }

    public float[] Embedding { get; set; }
}
```

**`ESearchFieldType` 枚举说明**：

| 枚举值         | 说明                 | 适用数据           |
| -------------- | -------------------- | ------------------ |
| `String`       | 文本搜索，支持分词器 | 标题、描述、内容   |
| `Autocomplete` | 自动补全，前缀匹配   | 搜索框实时建议     |
| `Number`       | 数值范围查询         | 价格、评分、年龄   |
| `Date`         | 日期范围查询         | 创建时间、发布时间 |
| `Boolean`      | 布尔精确匹配         | 是否上架、是否删除 |
| `ObjectId`     | ObjectId 精确匹配    | 关联 ID 字段       |
| `Geo`          | 地理图形搜索         | GeoJSON 坐标       |
| `Token`        | 无分词精确匹配       | SKU、标签、代码    |
| `Document`     | 嵌入文档搜索         | 嵌套对象           |

### VectorFieldAttribute —— 向量字段

标记存储 AI 嵌入向量的字段，配合 `[MongoSearchIndex(Type = ESearchIndexType.VectorSearch)]` 使用。

```csharp
[MongoSearchIndex(Name = "knowledge_vector", Type = ESearchIndexType.VectorSearch)]
public class KnowledgeBase
{
    public string Id { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string Category { get; set; }

    // OpenAI text-embedding-ada-002：1536 维，余弦相似度（最常用）
    [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine,
        IndexName = "knowledge_vector")]
    public float[] Embedding { get; set; }

    // 小型本地模型（如 sentence-transformers all-MiniLM），384 维
    // [VectorField(Dimensions = 384, Similarity = EVectorSimilarity.Cosine)]
    // public float[] LocalEmbedding { get; set; }
}
```

**`EVectorSimilarity` 枚举说明**：

| 枚举值       | 公式               | 适用场景                           |
| ------------ | ------------------ | ---------------------------------- |
| `Cosine`     | 余弦相似度（角度） | **推荐**，适合归一化的文本嵌入     |
| `DotProduct` | 点积（投影）       | 适合已充分训练且向量未归一化的场景 |
| `Euclidean`  | 欧几里得距离       | 图像、空间坐标等绝对距离敏感场景   |

### VectorFilterFieldAttribute —— 向量过滤字段

在向量搜索之前进行**预过滤**，只在满足条件的子集中执行向量相似度计算，提升准确性和性能。

```csharp
[MongoSearchIndex(Name = "doc_vector", Type = ESearchIndexType.VectorSearch)]
public class Document
{
    public string Id { get; set; }
    public string Title { get; set; }

    // 向量字段
    [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine,
        IndexName = "doc_vector")]
    public float[] Embedding { get; set; }

    // 过滤字段：先按分类过滤，再做向量搜索
    [VectorFilterField(IndexName = "doc_vector")]
    public string Category { get; set; }

    // 过滤字段：只在上架的文档中搜索
    [VectorFilterField(IndexName = "doc_vector")]
    public bool IsPublished { get; set; }

    // 过滤字段：日期范围过滤
    [VectorFilterField(IndexName = "doc_vector")]
    public DateTime PublishedAt { get; set; }
}
```

使用向量搜索（在 Controller 或 Service 中）：

```csharp
// 先用 AI 模型生成查询向量，再进行语义搜索
var queryVector = await embeddingService.GetEmbeddingAsync("MongoDB 如何使用事务？");

var pipeline = new BsonDocument[]
{
    new("$vectorSearch", new BsonDocument
    {
        { "index", "doc_vector" },
        { "path", "embedding" },
        { "queryVector", new BsonArray(queryVector.Select(f => (BsonValue)f)) },
        { "numCandidates", 150 },
        { "limit", 10 },
        // 预过滤：只搜索已发布的 "技术文档" 分类
        { "filter", new BsonDocument
            {
                { "category", "技术文档" },
                { "isPublished", true }
            }
        }
    })
};

var results = await db.Documents.Aggregate<BsonDocument>(pipeline).ToListAsync();
```

---

## 批量写入 Fluent API

`BulkOperationBuilder<T>` 提供链式调用风格的批量写入构建器，配合 `BulkWriteExtensions` 扩展方法使用。

**适用场景**：

- 数据迁移、批量导入
- 需要原子性的混合操作（插入 + 更新 + 删除）
- 性能要求高，需要一次网络往返完成多个操作

```csharp
// 一次网络请求完成：插入新订单 + 更新已有订单 + 删除过期订单
var result = await db.Orders.BulkWriteAsync(bulk => bulk
    // 插入新文档
    .InsertOne(new Order { UserId = "u1", Status = "pending", Amount = 99.9m })

    // 批量插入多个
    .InsertMany(newOrders)

    // 更新单个：将订单状态改为 shipped
    .UpdateOne(
        Builders<Order>.Filter.Eq(o => o.Id, "order_001"),
        Builders<Order>.Update.Set(o => o.Status, "shipped"))

    // upsert：存在则更新，不存在则插入
    .UpdateOne(
        Builders<Order>.Filter.Eq(o => o.OrderNo, "ON-20260219"),
        Builders<Order>.Update.SetOnInsert(o => o.CreatedAt, DateTime.UtcNow)
                              .Set(o => o.Status, "processing"),
        isUpsert: true)

    // 更新多个：将所有 "pending" 超过 7 天的订单标记为 "timeout"
    .UpdateMany(
        Builders<Order>.Filter.And(
            Builders<Order>.Filter.Eq(o => o.Status, "pending"),
            Builders<Order>.Filter.Lt(o => o.CreatedAt, DateTime.UtcNow.AddDays(-7))),
        Builders<Order>.Update.Set(o => o.Status, "timeout"))

    // 替换整个文档
    .ReplaceOne(
        Builders<Order>.Filter.Eq(o => o.Id, "order_002"),
        updatedOrder)

    // 删除单个
    .DeleteOne(Builders<Order>.Filter.Eq(o => o.Id, "expired_001"))

    // 删除多个：清理 3 年前的归档订单
    .DeleteMany(Builders<Order>.Filter.Lt(o => o.CreatedAt, DateTime.UtcNow.AddYears(-3))));

Console.WriteLine($"已插入: {result.InsertedCount}, 已更新: {result.ModifiedCount}, 已删除: {result.DeletedCount}");
```

---

## 聚合管道扩展

`AggregationExtensions` 对 `IMongoCollection<T>` 提供了常用聚合模式的快捷方法，避免手写 BsonDocument 管道。

### LookupAndUnwindAsync —— 关联查询（JOIN）

MongoDB 通过 `$lookup` 实现关联查询，再用 `$unwind` 展平数组，等效于 SQL 的 LEFT JOIN。

```csharp
// 查询订单，同时关联用户信息
// 等效 SQL: SELECT o.*, u.* FROM orders o LEFT JOIN users u ON o.userId = u._id
var results = await db.Orders.LookupAndUnwindAsync<User>(
    foreignCollectionName: "users",        // 关联的集合名
    localField: o => o.UserId,             // 本集合的连接字段
    foreignField: u => u.Id,               // 外集合的连接字段
    asField: "user",                       // 合并后的字段名
    filter: Builders<Order>.Filter.Eq(o => o.Status, "shipped"),  // 可选前置过滤
    preserveNullAndEmpty: true);           // true = LEFT JOIN；false = INNER JOIN

// results 是 BsonDocument 列表，包含订单字段 + 展平的用户信息
foreach (var doc in results)
{
    var orderId = doc["_id"].AsString;
    var userName = doc["user"]["name"].AsString;
}
```

### GroupByCountAsync —— 分组统计

```csharp
// 统计每种订单状态的数量，等效 SQL: SELECT status, COUNT(*) FROM orders GROUP BY status
var statusCounts = await db.Orders.GroupByCountAsync(
    groupByField: o => o.Status,
    filter: Builders<Order>.Filter.Gt(o => o.Amount, 0));  // 可选过滤

foreach (var (status, count) in statusCounts)
{
    Console.WriteLine($"{status}: {count} 条");
}
// 输出示例：
// shipped: 1230 条
// pending: 456 条
// cancelled: 88 条
```

### BucketAsync —— 区间分布统计

```csharp
// 统计订单金额的价格区间分布
var distribution = await db.Orders.BucketAsync(
    groupByField: o => o.Amount,
    boundaries: [0, 100, 500, 1000, 5000, (BsonValue)BsonMaxKey.Value],
    defaultBucket: "超出范围");

// 输出示例：
// 0-100: 320 条
// 100-500: 891 条
// 500-1000: 234 条
// 1000-5000: 56 条
```

### FacetAsync —— 多维度并行聚合

`$facet` 在一次查询中同时执行多个聚合管道，常用于构建电商搜索的多维度统计（价格区间 + 品牌分布 + 评分分布）：

```csharp
var facetResult = await db.Products.FacetAsync(new Dictionary<string, BsonDocument[]>
{
    // 面 1：按品牌统计数量
    ["byBrand"] =
    [
        new("$group", new BsonDocument { { "_id", "$brand" }, { "count", new BsonDocument("$sum", 1) } }),
        new("$sort", new BsonDocument("count", -1)),
        new("$limit", 10)
    ],
    // 面 2：按价格区间统计
    ["byPriceRange"] =
    [
        new("$bucket", new BsonDocument
        {
            { "groupBy", "$price" },
            { "boundaries", new BsonArray { 0, 100, 500, 1000, 5000 } },
            { "default", "5000+" },
            { "output", new BsonDocument("count", new BsonDocument("$sum", 1)) }
        })
    ]
});
```

---

## 地理空间查询

提供简化的 GeoJSON 工厂方法和扩展方法，让地理位置功能开箱即用。

> **前提**：集合中的地理字段需要有 `2dsphere` 索引（通过 `[MongoIndex(EIndexType.Geo2DSphere)]` 声明）。

### 实体定义

```csharp
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using MongoDB.Driver.GeoJsonObjectModel;

public class Store
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string City { get; set; }

    // 地理坐标：存储为 GeoJSON Point（经度, 纬度）
    // 注意：MongoDB GeoJSON 中经度在前、纬度在后
    [MongoIndex(EIndexType.Geo2DSphere)]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates> Location { get; set; }
}
```

### GeoPoint / GeoPolygon —— GeoJSON 工厂

```csharp
using EasilyNET.Mongo.Core.Geo;

// 创建点（经度, 纬度）：注意是 经度(longitude) 在前，纬度(latitude) 在后
var shanghaiPoint = GeoPoint.From(121.4737, 31.2304);       // 上海
var beijingPoint  = GeoPoint.From(116.4074, 39.9042);       // 北京

// 从元组创建
var point = GeoPoint.From((Longitude: 121.4737, Latitude: 31.2304));

// 创建多边形区域（闭合：首尾坐标必须相同）
var shanghaiArea = GeoPolygon.From(
    (120.85, 30.68),  // 西南点
    (122.20, 30.68),  // 东南点
    (122.20, 31.88),  // 东北点
    (120.85, 31.88),  // 西北点
    (120.85, 30.68)); // 回到起点（闭合）
```

### GeoQueryExtensions —— 地理查询过滤器

```csharp
using EasilyNET.Mongo.Core.Misc;
using EasilyNET.Mongo.Core.Geo;

// 查询 5 公里范围内的门店（NearSphere）
var nearbyFilter = GeoQueryExtensions.NearSphere<Store>(
    field: s => s.Location,
    longitude: 121.4737,        // 搜索中心：上海市中心
    latitude: 31.2304,
    maxDistanceMeters: 5000,    // 最大 5 公里
    minDistanceMeters: 100);    // 最小 100 米（排除太近的）

var nearbyStores = await db.Stores.Find(nearbyFilter).ToListAsync();

// 查询区域内的门店（GeoWithin，效率更高，但不返回距离）
var withinFilter = GeoQueryExtensions.GeoWithin<Store>(
    field: s => s.Location,
    polygon: shanghaiArea);

var storesInShanghai = await db.Stores.Find(withinFilter).ToListAsync();
```

### GeoNearAsync —— 附近查询（含距离）

`$geoNear` 管道阶段不仅能过滤附近的点，还会在结果中附加计算出的距离值：

```csharp
var nearbyWithDistance = await db.Stores.GeoNearAsync(
    field: s => s.Location,
    near: GeoPoint.From(121.4737, 31.2304),   // 搜索中心
    options: new GeoNearOptions
    {
        DistanceField   = "distance",            // 结果中距离字段名
        Spherical       = true,                  // 球面距离（更精确）
        MaxDistanceMeters = 10_000,             // 10 公里内
        MinDistanceMeters = 0,
        Limit           = 20,                   // 最多返回 20 条
        Filter          = new BsonDocument("city", "上海")  // 额外过滤：仅上海
    });

// 结果包含所有原始字段 + "distance" 字段（单位：米）
foreach (var doc in nearbyWithDistance)
{
    var name     = doc["name"].AsString;
    var distance = doc["distance"].AsDouble;
    Console.WriteLine($"{name}: {distance:F0} 米");
}
```

---

## Unwind 辅助类型

`UnwindObj<T>` 是配合 MongoDB `$unwind` 聚合操作的辅助类型，在展开数组字段时携带元数据。

```csharp
// 场景：订单中包含商品列表，需要将商品列表展开后查询
// 投影阶段使用 List<Item> 类型
var projection = Builders<Order>.Projection.Expression(o => new UnwindObj<List<OrderItem>>
{
    Obj   = o.Items,
    Count = o.Items.Count
});

// Unwind 展开后，每条结果对应一个 OrderItem，使用单个对象类型
// 此时 T 为 OrderItem（非 List）
```

---

## 完整示例

以下是一个包含多种特性的完整实体定义示例：

```csharp
using EasilyNET.Mongo.Core.Attributes;
using EasilyNET.Mongo.Core.Enums;
using MongoDB.Driver.GeoJsonObjectModel;

[MongoSearchIndex(Name = "product_search")]
[MongoSearchIndex(Name = "product_vector", Type = ESearchIndexType.VectorSearch)]
[MongoCompoundIndex(["categoryId", "price"],
    [EIndexType.Ascending, EIndexType.Ascending],
    Name = "idx_category_price")]
[MongoCompoundIndex(["sellerId", "status", "createdAt"],
    [EIndexType.Ascending, EIndexType.Ascending, EIndexType.Descending],
    Name = "idx_seller_status_time")]
public class Product
{
    public string Id { get; set; }

    [SearchField(ESearchFieldType.String, IndexName = "product_search", AnalyzerName = "lucene.chinese")]
    [SearchField(ESearchFieldType.Autocomplete, IndexName = "product_search", AnalyzerName = "lucene.chinese")]
    [MongoIndex(EIndexType.Text)]
    public string Name { get; set; }

    [SearchField(ESearchFieldType.String, IndexName = "product_search", AnalyzerName = "lucene.chinese")]
    public string Description { get; set; }

    [SearchField(ESearchFieldType.Number, IndexName = "product_search")]
    [MongoIndex(EIndexType.Ascending)]
    public decimal Price { get; set; }

    [MongoIndex(EIndexType.Ascending)]
    public string CategoryId { get; set; }

    [MongoIndex(EIndexType.Ascending)]
    public string SellerId { get; set; }

    [SearchField(ESearchFieldType.Token, IndexName = "product_search")]
    public string Sku { get; set; }

    [MongoIndex(EIndexType.Geo2DSphere)]
    public GeoJsonPoint<GeoJson2DGeographicCoordinates>? WarehouseLocation { get; set; }

    [MongoIndex(EIndexType.Descending)]
    public DateTime CreatedAt { get; set; }

    public string Status { get; set; }   // active / inactive / deleted

    [VectorField(Dimensions = 1536, Similarity = EVectorSimilarity.Cosine, IndexName = "product_vector")]
    public float[]? Embedding { get; set; }

    [VectorFilterField(IndexName = "product_vector")]
    public string? CategoryPath { get; set; }   // 分类路径，向量搜索时可预过滤品类
}
```
