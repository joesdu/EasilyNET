# GridFS 对象存储 API 设计方案

## 概述

本文档描述了在 `EasilyNET.Mongo.AspNetCore` 库中构建的高级 GridFS 对象存储 API 设计。该设计的目标是在 MongoDB GridFS 之上构建一层类似 S3/MinIO/Azure Blob Storage 的对象存储体验，同时保持"包装但不隐藏"的设计理念——用户始终可以访问底层的 `IGridFSBucket`。

---

## 核心目标

在 `IGridFSBucket` 之上构建一层薄薄的"对象存储"抽象：
- **Key/Metadata-first**：基于 key 的寻址 + 结构化元数据
- **Streaming-native**：上传/下载零缓冲，支持大文件流式传输
- **可选 HTTP 服务**：内置文件服务端点 + 签名 URL
- **不隐藏底层**：高级用户始终可以访问原始 `IGridFSBucket`

## 行动计划

| 步骤 | 内容 |
|------|------|
| 1 | 添加 `GridFSObjectStoreOptions` + DI 注册扩展（默认/命名存储） |
| 2 | 实现 `IGridFSObjectStore`（CRUD + 查询 + 元数据）和 `IGridFSBucketAccessor` |
| 3 | 通过 upload/download sessions 实现流式传输（零缓冲） |
| 4 | 定义元数据封装（类型化 + 自定义字典），映射到 GridFS `metadata` |
| 5 | 通过 keyed services + factory 添加多桶支持 |
| 6 | 添加可选的 ASP.NET Core 端点/中间件 + 可选 URL 签名 |
| 7 | 添加可选的索引/TTL 辅助方法（显式调用，非自动） |

**工作量评估**：中等（1-2 天）

---

## 1. 当前实现分析

现有的 GridFS 实现仅提供基本的 DI 注册功能：

```csharp
// 当前实现：只注册了原始驱动类型
services.AddGridFSBucket(opt => {
    opt.BucketName = "uploads";
    opt.ChunkSizeBytes = 512 * 1024;
});

// 用户必须直接使用 IGridFSBucket，承担所有复杂性：
// - 手动 ObjectId 解析
// - 手动构造 BsonDocument 元数据
// - 无文件列表/搜索抽象
// - 无流式传输优化
// - 无路径组织
// - 无 HTTP 集成
// - 无签名 URL
```

### 痛点对比

| 痛点 | S3/MinIO 体验 |
|------|--------------|
| 手动 `ObjectId.Parse` 寻址 | string key 直接使用 |
| 手动构造 `BsonDocument` 设元数据 | 结构化 metadata 对象 |
| 无文件查询/列表抽象 | ListObjects + prefix |
| 下载必须先写 MemoryStream | 直接返回可读 Stream |
| 无路径/目录组织 | prefix 模拟目录 |
| 无 HTTP 文件服务 | 内置 SAS URL / 端点 |
| 无版本管理 | 版本控制支持 |

---

## 2. 设计理念

在 `IGridFSBucket` 之上构建一层薄的"对象存储"抽象：

- **Key-first 寻址**：用 `string key`（类似 S3 的 object key）替代 `ObjectId` 作为主要寻址方式
- **Metadata-first**：结构化的元数据模型，content-type 自动检测
- **Streaming-native**：上传接受 `Stream`，下载直接返回可读 `Stream`（零缓冲）
- **不隐藏底层**：`IGridFSBucket` 始终可访问，高级用户可以 escape hatch
- **签名 URL**：通过 HMAC 签名 token + ASP.NET Core 中间件实现

---

## 3. 目录结构

```
EasilyNET.Mongo.AspNetCore/
├── GridFS/                              # 新增
│   ├── IGridFSObjectStore.cs            # 核心接口
│   ├── IGridFSObjectStoreFactory.cs     # 多桶工厂
│   ├── IGridFSUploadSession.cs          # 上传会话（大文件/进度）
│   ├── IGridFSDownloadSession.cs        # 下载会话（流式/Range）
│   ├── IGridFSKeyNormalizer.cs          # key 规范化接口
│   ├── IGridFSUrlSigner.cs              # 签名 URL 接口
│   ├── GridFSObjectStore.cs             # 默认实现（internal）
│   ├── GridFSObjectInfo.cs               # 文件信息 record
│   ├── GridFSObjectStoreOptions.cs       # 存储配置
│   ├── GridFSPutOptions.cs               # 上传选项
│   ├── GridFSOpenReadOptions.cs          # 下载选项
│   ├── GridFSListRequest.cs              # 列表/查询请求
│   ├── GridFSMetadataPatch.cs            # 元数据更新
│   └── GridFSTransferProgress.cs         # 进度报告
├── Extensions/
│   ├── GridFSServiceExtensions.cs        # 已有，扩展
│   └── GridFSEndpointExtensions.cs       # 新增，Minimal API 端点
```

---

## 4. 核心接口

### 4.1 IGridFSObjectStore

```csharp
namespace EasilyNET.Mongo.AspNetCore.GridFS;

public interface IGridFSObjectStore
{
    /// <summary>存储名称（多桶场景）。<para>Store name for multi-bucket.</para></summary>
    string Name { get; }

    /// <summary>底层 GridFSBucket（不隐藏驱动类型）。<para>Raw IGridFSBucket escape hatch.</para></summary>
    IGridFSBucket Bucket { get; }

    /// <summary>底层数据库。<para>Raw IMongoDatabase.</para></summary>
    IMongoDatabase Database { get; }

    // ── Key-first CRUD ────

    Task<GridFSObjectInfo> PutAsync(string key, Stream content,
        GridFSPutOptions? options = null, CancellationToken ct = default);

    Task<IGridFSUploadSession> StartUploadAsync(string key,
        GridFSStartUploadOptions? options = null, CancellationToken ct = default);

    Task<IGridFSDownloadSession> OpenReadAsync(string key,
        GridFSOpenReadOptions? options = null, CancellationToken ct = default);

    Task<GridFSObjectInfo?> GetInfoAsync(string key,
        GridFSGetOptions? options = null, CancellationToken ct = default);

    ValueTask<bool> ExistsAsync(string key, CancellationToken ct = default);

    Task<bool> DeleteAsync(string key,
        GridFSDeleteOptions? options = null, CancellationToken ct = default);

    // ──── Metadata ────

    Task<GridFSObjectInfo> UpsertMetadataAsync(string key, GridFSMetadataPatch patch,
        CancellationToken ct = default);

    Task<GridFSObjectInfo> SetTagsAsync(string key, IReadOnlyCollection<string> tags,
        CancellationToken ct = default);

    // ──── List / Search ────

    IAsyncEnumerable<GridFSObjectInfo> ListAsync(GridFSListRequest? request = null,
        CancellationToken ct = default);

    Task<long> CountAsync(GridFSListRequest? request = null,
        CancellationToken ct = default);

    // ──── Versioning ────

    IAsyncEnumerable<GridFSObjectInfo> ListVersionsAsync(string key,
        int limit = 100, CancellationToken ct = default);

    // ──── Id-addressable (escape hatch) ────

    Task<IGridFSDownloadSession> OpenReadAsync(ObjectId id,
        GridFSOpenReadOptions? options = null, CancellationToken ct = default);

    Task<bool> DeleteAsync(ObjectId id, CancellationToken ct = default);
}
```

设计要点：
- **两种上传路径**：`PutAsync`（便捷，一次性）和 `StartUploadAsync`（大文件，支持进度、可中止）
- **两种寻址**：string key（主要）和 ObjectId（escape hatch）
- **返回 `IAsyncEnumerable`**：列表操作天然支持流式消费

### 4.2 流式会话接口

```csharp
/// <summary>上传会话：写入 Stream → CompleteAsync 生成版本。</summary>
public interface IGridFSUploadSession : IAsyncDisposable
{
    string Key { get; }
    Stream Stream { get; }
    Task<GridFSObjectInfo> CompleteAsync(CancellationToken ct = default);
    Task AbortAsync(CancellationToken ct = default);
}

/// <summary>下载会话：可读 Stream + 对象信息（支持 Range/Seek）。</summary>
public interface IGridFSDownloadSession : IAsyncDisposable
{
    GridFSObjectInfo Info { get; }
    Stream Stream { get; }
}
```

### 4.3 多桶工厂

```csharp
/// <summary>多桶工厂（与 keyed services 配合使用）。</summary>
public interface IGridFSObjectStoreFactory
{
    /// <summary>获取指定名称的对象存储。</summary>
    IGridFSObjectStore GetRequired(string name);
}
```

---

## 5. 数据模型

### 5.1 GridFSObjectInfo

```csharp
/// <summary>对象信息（类似 S3 ObjectInfo）。</summary>
public sealed record GridFSObjectInfo(
    ObjectId Id,
    string Key,
    string FileName,
    long Length,
    DateTimeOffset UploadedAt,
    string? ContentType,
    string? ContentDisposition,
    string? CacheControl,
    string? Md5,
    string ETag,                                    // 基于 ObjectId 生成
    DateTimeOffset? ExpiresAt,
    IReadOnlyCollection<string> Tags,
    IReadOnlyDictionary<string, string> CustomMetadata
);
```

### 5.2 GridFSTransferProgress

```csharp
/// <summary>进度报告。</summary>
public sealed record GridFSTransferProgress(
    long BytesTransferred,
    long? TotalBytes,
    TimeSpan Elapsed
);
```

---

## 6. 操作选项

### 6.1 GridFSPutOptions

```csharp
public enum GridFSPutMode
{
    /// <summary>若存在则失败。</summary>
    FailIfExists = 0,

    /// <summary>覆盖（删除旧版本）。</summary>
    Overwrite = 1,

    /// <summary>创建新版本（保留旧版本）。</summary>
    CreateNewVersion = 2,
}

public sealed record GridFSPutOptions
{
    public GridFSPutMode Mode { get; init; } = GridFSPutMode.CreateNewVersion;
    public string? FileName { get; init; }          // 原始文件名（与 key 可不同）
    public string? ContentType { get; init; }       // null 时从 key 扩展名自动检测
    public string? ContentDisposition { get; init; }
    public string? CacheControl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public IReadOnlyCollection<string>? Tags { get; init; }
    public IReadOnlyDictionary<string, string>? CustomMetadata { get; init; }
    public IProgress<GridFSTransferProgress>? Progress { get; init; }
}
```

### 6.2 GridFSOpenReadOptions

```csharp
public sealed record GridFSOpenReadOptions
{
    /// <summary>指定版本，默认最新。</summary>
    public ObjectId? VersionId { get; init; }

    /// <summary>HTTP Range 支持：起始偏移。</summary>
    public long? RangeStart { get; init; }

    /// <summary>HTTP Range 支持：结束偏移（含）。</summary>
    public long? RangeEndInclusive { get; init; }
}
```

### 6.3 GridFSDeleteOptions

```csharp
public sealed record GridFSDeleteOptions
{
    public ObjectId? VersionId { get; init; }

    /// <summary>删除该 key 的全部版本。</summary>
    public bool DeleteAllVersions { get; init; }
}
```

### 6.4 GridFSListRequest

```csharp
public sealed record GridFSListRequest
{
    /// <summary>前缀过滤（目录风格）。</summary>
    public string? Prefix { get; init; }

    /// <summary>标签过滤（AND 逻辑）。</summary>
    public IReadOnlyCollection<string>? TagsAll { get; init; }

    /// <summary>内容类型过滤（"image/" 匹配所有图片）。</summary>
    public string? ContentType { get; init; }

    /// <summary>文件大小范围。</summary>
    public long? SizeMin { get; init; }
    public long? SizeMax { get; init; }

    /// <summary>上传时间范围。</summary>
    public DateTimeOffset? UploadedAfter { get; init; }
    public DateTimeOffset? UploadedBefore { get; init; }

    /// <summary>自定义元数据过滤。</summary>
    public IReadOnlyDictionary<string, string>? CustomMetadata { get; init; }

    /// <summary>分页参数。</summary>
    public int Limit { get; init; } = 100;
    public int Skip { get; init; }

    /// <summary>排序方式。</summary>
    public GridFSSort Sort { get; init; } = GridFSSort.UploadedAtDesc;
}

public enum GridFSSort
{
    UploadedAtDesc = 0,
    UploadedAtAsc = 1,
    SizeDesc = 2,
    SizeAsc = 3,
}
```

### 6.5 GridFSMetadataPatch

```csharp
public sealed record GridFSMetadataPatch
{
    public string? ContentType { get; init; }
    public string? CacheControl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>替换标签。</summary>
    public IReadOnlyCollection<string>? TagsSet { get; init; }

    /// <summary>追加标签。</summary>
    public IReadOnlyCollection<string>? TagsAdd { get; init; }

    /// <summary>移除标签。</summary>
    public IReadOnlyCollection<string>? TagsRemove { get; init; }

    /// <summary>合并自定义元数据。</summary>
    public IReadOnlyDictionary<string, string>? CustomMetadataUpsert { get; init; }

    /// <summary>移除自定义元数据键。</summary>
    public IReadOnlyCollection<string>? CustomMetadataRemoveKeys { get; init; }
}
```

---

## 7. 配置

### 7.1 GridFSObjectStoreOptions

```csharp
public sealed class GridFSObjectStoreOptions
{
    public const string DefaultName = "default";

    public string BucketName { get; set; } = "fs";
    public int ChunkSizeBytes { get; set; } = 261120;

    /// <summary>key 存储策略。</summary>
    public GridFSKeyStrategy KeyStrategy { get; set; } = GridFSKeyStrategy.Metadata;

    /// <summary>自动检测 content-type。</summary>
    public bool AutoDetectContentType { get; set; } = true;

    public string DefaultContentType { get; set; } = "application/octet-stream";

    /// <summary>key 规范化器（路径校验、去重斜杠等）。</summary>
    public IGridFSKeyNormalizer? KeyNormalizer { get; set; }

    /// <summary>索引配置。</summary>
    public GridFSIndexOptions Indexes { get; set; } = new();

    /// <summary>HTTP 服务配置。</summary>
    public GridFSHttpServingOptions Http { get; set; } = new();

    /// <summary>签名 URL 配置。</summary>
    public GridFSUrlSigningOptions UrlSigning { get; set; } = new();
}

public enum GridFSKeyStrategy
{
    /// <summary>key 存于 metadata（推荐）。</summary>
    Metadata = 0,

    /// <summary>key 直接用作 filename。</summary>
    FileName = 1,
}
```

### 7.2 GridFSIndexOptions

```csharp
public sealed class GridFSIndexOptions
{
    /// <summary>启用 TTL（需要 expiresAt 字段）。</summary>
    public bool EnableTtlOnExpiresAt { get; set; }

    /// <summary>TTL 索引秒数。</summary>
    public int ExpireAfterSeconds { get; set; } = 0;

    /// <summary>创建推荐索引。</summary>
    public bool EnsureRecommendedIndexes { get; set; } = true;
}
```

---

## 8. 元数据存储策略

元数据存储在 GridFS `files.metadata` 字段中：

```json
{
  "key": "images/avatars/user_001.jpg",
  "contentType": "image/jpeg",
  "cacheControl": "public, max-age=31536000",
  "tags": ["avatar", "user_001"],
  "expiresAt": ISODate("2026-03-20T00:00:00Z"),
  "custom": {
    "uploadedBy": "user_001",
    "department": "engineering"
  }
}
```

- `key` 是主要寻址字段（建索引）
- `tags` 是数组，支持 `$all` / `$in` 查询
- `expiresAt` 可配 TTL 索引实现自动过期清理
- `custom` 是 `{ string: string }` 字典

---

## 9. 多桶支持

```csharp
// 注册
builder.Services.AddGridFSObjectStore(opt => {
    opt.BucketName = "uploads";
    opt.AutoDetectContentType = true;
});

builder.Services.AddGridFSObjectStore("media", opt => {
    opt.BucketName = "media";
    opt.ChunkSizeBytes = 1024 * 1024;  // 1MB chunks for large files
});

// 注入
public class FileService(IGridFSObjectStore store) { }
public class MediaService([FromKeyedServices("media")] IGridFSObjectStore store) { }

// 工厂（动态解析）
public class DynamicService(IGridFSObjectStoreFactory factory)
{
    public async Task Upload(string bucket, string key, Stream content)
    {
        var store = factory.GetRequired(bucket);
        await store.PutAsync(key, content);
    }
}
```

---

## 10. ASP.NET Core 集成

### 10.1 文件服务端点

```csharp
var app = builder.Build();

// 下载端点：GET /gridfs/{**key}，支持 Range/ETag/Content-Disposition
app.MapGridFSDownload("/gridfs/{**key}");

// 上传端点：PUT /gridfs/{**key}，流式写入
app.MapGridFSUpload("/gridfs/{**key}");

// 签名 URL 下载：GET /gridfs/_signed/{token}
app.MapGridFSSignedDownload("/gridfs/_signed/{token}");

// 显式初始化索引
app.UseGridFSEnsureIndexes();
```

### 10.2 GridFSHttpServingOptions

```csharp
public sealed class GridFSHttpServingOptions
{
    /// <summary>下载端点基路径。</summary>
    public string DownloadRoutePrefix { get; set; } = "/gridfs";

    /// <summary>启用 Range（视频/大文件）。</summary>
    public bool EnableRangeProcessing { get; set; } = true;

    /// <summary>设置 ETag/Last-Modified。</summary>
    public bool EnableCachingHeaders { get; set; } = true;

    /// <summary>下载时的默认 Content-Disposition。</summary>
    public GridFSContentDispositionMode ContentDispositionMode { get; init; } = GridFSContentDispositionMode.Inline;
}
```

---

## 11. 签名 URL

### 11.1 接口定义

```csharp
public interface IGridFSUrlSigner
{
    /// <summary>生成签名下载 URL。</summary>
    string CreateDownloadUrl(string key, TimeSpan? expiration = null);

    /// <summary>验证签名并提取信息。</summary>
    bool TryValidate(string token, out GridFSValidatedToken validated);
}

public sealed record GridFSValidatedToken(
    string StoreName,
    string Key,
    DateTimeOffset ExpiresAt,
    string? ETag
);
```

### 11.2 GridFSUrlSigningOptions

```csharp
public sealed class GridFSUrlSigningOptions
{
    public bool Enabled { get; set; }
    public TimeSpan DefaultLifetime { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>HMAC 密钥（由宿主提供）。</summary>
    public byte[]? HmacKey { get; set; }
}
```

### 11.3 使用示例

```csharp
public class ShareService(IGridFSUrlSigner signer)
{
    public string CreateShareLink(string key)
        => signer.CreateDownloadUrl(key, TimeSpan.FromHours(24));
}
```

实现原理：HMAC-SHA256 签名 `{storeName}:{key}:{expiresAt}`，中间件验证 token 后从 GridFS 流式返回内容。

---

## 12. Key 规范化

```csharp
public interface IGridFSKeyNormalizer
{
    /// <summary>规范化 key 并校验合法性。</summary>
    bool TryNormalize(string key, [NotNullWhen(true)] out string? normalized, out string? error);
}
```

默认实现应：
- 拒绝 `..`（防止路径穿越）
- 拒绝反斜杠 `\`
- 合并重复斜杠 `//` → `/`
- 去除首尾斜杠
- 限制最大长度

---

## 13. 完整使用示例

```csharp
// ── Program.cs ──
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMongoContext<MyDbContext>(builder.Configuration);
builder.Services.AddGridFSObjectStore(opt =>
{
    opt.BucketName = "uploads";
    opt.AutoDetectContentType = true;
    opt.Indexes.EnableTtlOnExpiresAt = true;
    opt.UrlSigning.Enabled = true;
    opt.UrlSigning.HmacKey = Convert.FromBase64String(builder.Configuration["GridFS:SigningKey"]!);
});

var app = builder.Build();
app.UseGridFSEnsureIndexes();
app.MapGridFSDownload();
app.MapGridFSUpload();
app.MapGridFSSignedDownload();
app.Run();

// ── Controller / Service ──
public class DocumentService(IGridFSObjectStore store, IGridFSUrlSigner signer)
{
    // 上传文档
    public async Task<GridFSObjectInfo> UploadAsync(string userId, IFormFile file, CancellationToken ct)
    {
        var key = $"documents/{userId}/{file.FileName}";
        await using var stream = file.OpenReadStream();
        return await store.PutAsync(key, stream, new()
        {
            Mode = GridFSPutMode.CreateNewVersion,
            Tags = ["document", userId],
            CustomMetadata = new Dictionary<string, string> { ["uploadedBy"] = userId },
            ExpiresAt = DateTimeOffset.UtcNow.AddYears(1),
        }, ct);
    }

    // 流式下载（大文件友好）
    public async Task<IGridFSDownloadSession> DownloadAsync(string key, CancellationToken ct)
        => await store.OpenReadAsync(key, ct: ct);

    // 列出用户文档
    public IAsyncEnumerable<GridFSObjectInfo> ListUserDocsAsync(string userId, CancellationToken ct)
        => store.ListAsync(new() { Prefix = $"documents/{userId}/", TagsAll = [userId] }, ct);

    // 生成分享链接
    public string CreateShareLink(string key)
        => signer.CreateDownloadUrl(key, TimeSpan.FromDays(7));

    // 查看版本历史
    public IAsyncEnumerable<GridFSObjectInfo> GetVersionsAsync(string key, CancellationToken ct)
        => store.ListVersionsAsync(key, ct: ct);
}
```

---

## 14. 向后兼容性

- 现有的 `AddGridFSBucket()` 保持不变，向后兼容
- `AddGridFSObjectStore()` 内部会注册 `IGridFSBucket`，两者可共存
- `IGridFSObjectStore.Bucket` 暴露底层类型，高级用户随时可以 escape

---

## 15. 实现优先级

| 阶段 | 内容 | 工作量 |
|------|------|--------|
| P0 | `IGridFSObjectStore` 核心 CRUD + 元数据 + 列表 | ~1d |
| P0 | `GridFSObjectStoreOptions` + DI 注册 | ~0.5d |
| P1 | Upload/Download Session（大文件流式） | ~0.5d |
| P1 | 多桶 keyed services + factory | ~0.5d |
| P2 | Minimal API 端点（下载/上传/Range） | ~0.5d |
| P2 | 签名 URL | ~0.5d |
| P3 | 索引管理 + TTL | ~0.25d |
| P3 | Key normalizer 默认实现 | ~0.25d |

**总计约 3-4 天的工作量**

---

## 16. 版本与命名策略

- **Object identity**: `key` (string) 是稳定的对象标识符（如 `avatars/u123.png`、`docs/2026/contract.pdf`）
- **GridFS filename**: 可以是原始客户端文件名或规范化后的 key，通过 `GridFSKeyStrategy` 配置
- **Versioning**: 每次上传产生新的 `ObjectId` 版本；"latest" 通过查询排序 `uploadDate` + `_id` 解析

---

## 17. 索引策略

默认创建的推荐索引：

```csharp
// key 索引（主查询）
{ "metadata.key": 1 }

// 标签索引（支持 $all / $in 查询）
{ "metadata.tags": 1 }

// 内容类型索引
{ "metadata.contentType": 1 }

// 上传时间索引（排序）
{ "uploadDate": -1 }

// TTL 索引（可选）
{ "metadata.expiresAt": 1 }  // expireAfterSeconds: 0
```

---

## 18. 文件命名策略

通过 `IGridFSKeyNormalizer` 接口实现：

```csharp
public sealed class DefaultGridFSKeyNormalizer : IGridFSKeyNormalizer
{
    private const int MaxKeyLength = 1024;

    public bool TryNormalize(string key, out string? normalized, out string? error)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            error = "Key cannot be empty";
            normalized = null;
            return false;
        }

        // 去除首尾空白
        normalized = key.Trim();

        // 拒绝路径穿越
        if (normalized.Contains(".."))
        {
            error = "Key cannot contain '..'";
            normalized = null;
            return false;
        }

        // 拒绝反斜杠
        if (normalized.Contains('\\'))
        {
            error = "Key cannot contain backslash";
            normalized = null;
            return false;
        }

        // 合并重复斜杠
        while (normalized.Contains("//"))
            normalized = normalized.Replace("//", "/");

        // 去除首尾斜杠
        normalized = normalized.Trim('/');

        // 限制长度
        if (normalized.Length > MaxKeyLength)
        {
            error = $"Key exceeds maximum length of {MaxKeyLength}";
            normalized = null;
            return false;
        }

        error = null;
        return true;
    }
}
```
