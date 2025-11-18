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

GridFS 是 MongoDB 的分布式文件系统，支持存储超过 16MB 的文件。本实现经过优化，支持高效的流式传输和范围读取。

### 基础使用

1. **注册服务**:

```csharp
// 需要提前注册 IMongoDatabase，或使用其他重载
builder.Services.AddMongoGridFS(options =>
{
    options.ChunkSizeBytes = 255 * 1024; // 255KB - 优化流式传输性能
});
```

2. **依赖注入使用**:

```csharp
public class FileService(IGridFSBucket bucket)
{
    private readonly IGridFSBucket _bucket = bucket;

    public async Task<ObjectId> UploadFileAsync(Stream stream, string filename)
    {
        var id = await _bucket.UploadFromStreamAsync(filename, stream);
        return id;
    }

    public async Task<Stream> DownloadFileAsync(string filename)
    {
        return await _bucket.OpenDownloadStreamByNameAsync(filename);
    }
}
```

### 🎬 流式传输 - 视频/音频播放

支持 HTTP Range 请求的流式传输,完美支持视频播放器的进度拖动和断点续传。

#### 服务端实现

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

[HttpGet("StreamRange/{id}")]
public async Task<IActionResult> StreamVideo(string id, CancellationToken cancellationToken)
{
    // 解析 Range 头 (e.g., "bytes=1024-2047")
    var rangeHeader = Request.Headers[HeaderNames.Range].ToString();
    long? startByte = null;
    long? endByte = null;

    if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
    {
        var range = rangeHeader[6..].Split('-');
        if (range.Length == 2)
        {
            if (long.TryParse(range[0], out var start))
                startByte = start;
            if (!string.IsNullOrEmpty(range[1]) && long.TryParse(range[1], out var end))
                endByte = end;
        }
    }

    var result = await GridFSRangeStreamHelper.DownloadRangeAsync(
        bucket,
        ObjectId.Parse(id),
        startByte ?? 0,
        endByte,
        cancellationToken
    );

    var contentType = result.FileInfo.Metadata?.Contains("contentType") == true
        ? result.FileInfo.Metadata["contentType"].AsString
        : "application/octet-stream";

    // 设置响应头支持范围请求
    Response.Headers[HeaderNames.AcceptRanges] = "bytes";

    // 根据是否有 Range 头决定状态码
    if (startByte.HasValue || endByte.HasValue)
    {
        Response.StatusCode = 206; // 206 Partial Content
        Response.Headers[HeaderNames.ContentRange] =
            $"bytes {result.RangeStart}-{result.RangeEnd}/{result.TotalLength}";
    }

    return File(result.Stream, contentType, result.FileInfo.Filename, enableRangeProcessing: true);
}
```

#### 客户端示例

##### HTML5 Video/Audio (自动支持)

```html
<!-- 视频播放器会自动发送 Range 请求支持拖动进度 -->
<video controls width="800">
  <source
    src="/api/GridFS/StreamRange/507f1f77bcf86cd799439011"
    type="video/mp4"
  />
  您的浏览器不支持视频播放
</video>

<!-- 音频播放器同理 -->
<audio controls>
  <source
    src="/api/GridFS/StreamRange/507f1f77bcf86cd799439012"
    type="audio/mpeg"
  />
  您的浏览器不支持音频播放
</audio>
```

##### JavaScript 手动控制下载断点续传

```typescript
import { GridFSResumableDownloader } from "./gridfs-resumable";

const downloader = new GridFSResumableDownloader({
  downloadUrl: "/api/GridFS/StreamRange",
  fileId: "507f1f77bcf86cd799439011",
  filename: "video.mp4",
  onProgress: (progress) => {
    console.log(`下载进度: ${progress.percentage.toFixed(2)}%`);
    console.log(
      `已下载: ${formatFileSize(progress.loaded)} / ${formatFileSize(
        progress.total
      )}`
    );
  },
  onError: (error) => {
    console.error("下载失败:", error);
    // 可以调用 downloader.start() 重新开始断点续传
  },
});

// 开始下载
try {
  await downloader.downloadAndSave();
  console.log("下载完成!");
} catch (error) {
  // 网络中断,稍后可以重新调用 downloader.start() 继续下载
  console.error("下载中断:", error);
}
```

#### 优势

- ✅ **节省带宽**: 只传输需要的部分,无需下载整个文件
- ✅ **快速响应**: 支持从任意位置开始播放,<100ms 起播延迟
- ✅ **断点续传**: 网络中断后可从断点继续,不会重复下载
- ✅ **内存优化**: 流式处理,不会一次性加载整个文件到内存
- ✅ **兼容性强**: 标准 HTTP Range 协议,所有现代浏览器原生支持

### ⚡ 批量上传优化

使用优化的块大小和并行处理提升批量上传性能。

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;

// 单文件优化上传 - 自动根据文件大小选择最佳块大小
var fileId = await GridFSUploadHelper.UploadOptimizedAsync(
    bucket,
    "video.mp4",
    fileStream,
    new GridFSUploadOptions
    {
        Metadata = new BsonDocument
        {
            { "contentType", "video/mp4" },
            { "userId", "user123" }
        }
    }
);

// 批量并行上传 - 充分利用多核 CPU
var files = new List<(string Filename, Stream Source, Dictionary<string, object>? Metadata)>
{
    ("file1.mp4", stream1, new() { { "contentType", "video/mp4" } }),
    ("file2.jpg", stream2, new() { { "contentType", "image/jpeg" } }),
    ("file3.pdf", stream3, new() { { "contentType", "application/pdf" } })
};

var fileIds = await GridFSUploadHelper.UploadManyAsync(
    bucket,
    files,
    maxDegreeOfParallelism: 4 // 使用 4 个并行任务
);
```

### 📤 断点续传上传 - 大文件分块上传

支持超大文件的分块上传和断点续传,适合不稳定网络环境。前后端配合实现真正的断点续传。

#### 🔧 核心特性

- ✅ **分块上传**: 将大文件切分成小块,支持并发上传
- ✅ **断点续传**: 网络中断后可继续上传,不会重复上传已完成的块
- ✅ **进度跟踪**: 实时显示上传进度、速度和预计剩余时间
- ✅ **暂停恢复**: 支持暂停和恢复上传操作
- ✅ **完整性验证**: 支持文件 Hash 验证,确保文件完整
- ✅ **会话管理**: 自动清理过期会话,防止垃圾数据堆积

#### 服务端实现

##### 1. 创建控制器

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;
using EasilyNET.Mongo.AspNetCore.Models;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

[ApiController]
[Route("api/GridFS/Resumable")]
public class GridFSResumableController : ControllerBase
{
    private readonly GridFSResumableUploadHelper _uploadHelper;
    private readonly ILogger<GridFSResumableController> _logger;

    public GridFSResumableController(IGridFSBucket bucket, ILogger<GridFSResumableController> logger)
    {
        _uploadHelper = new GridFSResumableUploadHelper(bucket);
        _logger = logger;
    }

    /// <summary>
    /// 初始化上传会话
    /// </summary>
    [HttpPost("CreateSession")]
    public async Task<IActionResult> CreateSession([FromBody] InitializeUploadRequest request)
    {
        try
        {
            var metadata = new BsonDocument
            {
                { "contentType", request.ContentType },
                { "userId", User.Identity?.Name ?? "anonymous" },
                { "uploadTime", DateTime.UtcNow }
            };

            // 添加自定义元数据
            if (request.Metadata != null)
            {
                foreach (var (key, value) in request.Metadata)
                {
                    metadata[key] = BsonValue.Create(value);
                }
            }

            var session = await _uploadHelper.CreateSessionAsync(
                request.Filename,
                request.Size,
                metadata,
                request.ChunkSize,
                sessionExpirationHours: 24 // 会话 24 小时后过期
            );

            _logger.LogInformation("创建上传会话: {SessionId}, 文件: {Filename}, 大小: {Size}",
                session.SessionId, request.Filename, request.Size);

            return Ok(new { uploadId = session.SessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化上传失败");
            return StatusCode(500, new { error = "初始化上传失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 上传分块
    /// </summary>
    [HttpPost("UploadChunk")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 限制单个分块最大 10MB
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> UploadChunk()
    {
        try
        {
            var uploadId = Request.Form["uploadId"].ToString();
            if (string.IsNullOrEmpty(uploadId))
                return BadRequest(new { error = "缺少 uploadId 参数" });

            if (!int.TryParse(Request.Form["chunkIndex"].ToString(), out var chunkIndex))
                return BadRequest(new { error = "无效的 chunkIndex 参数" });

            if (Request.Form.Files.Count == 0)
                return BadRequest(new { error = "未找到上传的文件块" });

            var chunkFile = Request.Form.Files[0];

            using var ms = new MemoryStream();
            await chunkFile.CopyToAsync(ms);
            var chunkData = ms.ToArray();

            var session = await _uploadHelper.UploadChunkAsync(uploadId, chunkIndex, chunkData);

            _logger.LogDebug("上传分块: {UploadId}, 块 {ChunkIndex}, 进度: {Progress:F2}%",
                uploadId, chunkIndex, (double)session.UploadedSize / session.TotalSize * 100);

            return Ok(new
            {
                uploadedChunks = session.UploadedChunks.Count,
                totalSize = session.UploadedSize,
                progress = (double)session.UploadedSize / session.TotalSize * 100,
                status = session.Status.ToString()
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "上传分块失败");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传分块时发生错误");
            return StatusCode(500, new { error = "上传分块失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 完成上传
    /// </summary>
    [HttpPost("Finalize/{uploadId}")]
    public async Task<IActionResult> FinalizeUpload(string uploadId, [FromBody] CompleteUploadRequest request)
    {
        try
        {
            var fileId = await _uploadHelper.FinalizeUploadAsync(
                request.UploadId,
                request.FileHash // 可选: 验证文件完整性
            );

            _logger.LogInformation("上传完成: {UploadId}, 文件 ID: {FileId}", request.UploadId, fileId);

            return Ok(new { fileId = fileId.ToString(), success = true });
        }
        catch (InvalidOperationException ex)
        {
            // 检查缺失的块
            var missingChunks = await _uploadHelper.GetMissingChunksAsync(request.UploadId);
            _logger.LogWarning("上传未完成,缺少 {Count} 个分块: {UploadId}", missingChunks.Count, request.UploadId);

            return BadRequest(new
            {
                error = ex.Message,
                missingChunks,
                success = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "完成上传时发生错误");
            return StatusCode(500, new { error = "完成上传失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 取消上传
    /// </summary>
    [HttpDelete("Cancel/{uploadId}")]
    public async Task<IActionResult> CancelUpload(string uploadId)
    {
        try
        {
            await _uploadHelper.CancelSessionAsync(request.UploadId);
            _logger.LogInformation("取消上传: {UploadId}", request.UploadId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消上传时发生错误");
            return StatusCode(500, new { error = "取消上传失败", message = ex.Message });
        }
    }

    /// <summary>
    /// 获取上传进度
    /// </summary>
    [HttpGet("Session/{uploadId}")]
    public async Task<IActionResult> GetSession(string uploadId)
    {
        try
        {
            var session = await _uploadHelper.GetSessionAsync(uploadId);
            if (session == null)
                return NotFound(new { error = "上传会话不存在或已过期" });

            var missingChunks = await _uploadHelper.GetMissingChunksAsync(uploadId);

            return Ok(new
            {
                session.SessionId,
                session.Filename,
                session.TotalSize,
                session.UploadedSize,
                Progress = (double)session.UploadedSize / session.TotalSize * 100,
                Status = session.Status.ToString(),
                UploadedChunks = session.UploadedChunks.Count,
                MissingChunks = missingChunks,
                session.CreatedAt,
                session.UpdatedAt,
                session.ExpiresAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取上传状态时发生错误");
            return StatusCode(500, new { error = "获取状态失败", message = ex.Message });
        }
    }
}

// DTO 类
public record InitializeUploadRequest(
    string Filename,
    long Size,
    string ContentType,
    int? ChunkSize,
    Dictionary<string, object>? Metadata = null
);

public record CompleteUploadRequest(string UploadId, string? FileHash = null);
public record AbortUploadRequest(string UploadId);
```

##### 2. 配置服务

```csharp
var builder = WebApplication.CreateBuilder(args);

// 配置请求大小限制
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB 分块
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024; // 10MB
});

// 注册 GridFS
builder.Services.AddMongoGridFS();
```

##### 3. 后台任务清理过期会话

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;

public class CleanupExpiredSessionsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CleanupExpiredSessionsBackgroundService> _logger;

    public CleanupExpiredSessionsBackgroundService(
        IServiceProvider services,
        ILogger<CleanupExpiredSessionsBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var bucket = scope.ServiceProvider.GetRequiredService<IGridFSBucket>();
                var uploadHelper = new GridFSResumableUploadHelper(bucket);

                await uploadHelper.CleanupExpiredSessionsAsync(stoppingToken);
                _logger.LogInformation("清理过期上传会话完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期会话时发生错误");
            }

            // 每小时执行一次
            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }
}

// 在 Program.cs 中注册
builder.Services.AddHostedService<CleanupExpiredSessionsBackgroundService>();
```

#### 前端实现

##### 使用 TypeScript 库

将提供的 `gridfs-resumable.ts` 文件引入项目:

```typescript
import {
  GridFSResumableUploader,
  formatFileSize,
  formatTime,
} from "./gridfs-resumable";

// HTML 文件选择
const fileInput = document.getElementById("fileInput") as HTMLInputElement;
const progressBar = document.getElementById("progress") as HTMLProgressElement;
const statusText = document.getElementById("status") as HTMLDivElement;

fileInput.addEventListener("change", async (e) => {
  const file = (e.target as HTMLInputElement).files?.[0];
  if (!file) return;

  const uploader = new GridFSResumableUploader(file, {
    uploadUrl: "/api/gridfsupload",
    chunkSize: 1024 * 1024, // 1MB per chunk
    maxConcurrent: 3, // 同时上传 3 个块
    onProgress: (progress) => {
      progressBar.value = progress.percentage;
      statusText.innerHTML = `
                上传进度: ${progress.percentage.toFixed(2)}%<br>
                已上传: ${formatFileSize(progress.loaded)} / ${formatFileSize(
        progress.total
      )}<br>
                速度: ${formatFileSize(progress.speed)}/s<br>
                预计剩余时间: ${formatTime(progress.remainingTime)}
            `;
    },
    onError: (error) => {
      console.error("上传失败:", error);
      statusText.textContent = `上传失败: ${error.message}`;
    },
    onComplete: (fileId) => {
      statusText.textContent = `上传完成! 文件 ID: ${fileId}`;
    },
  });

  try {
    const fileId = await uploader.start();
    console.log("文件上传完成:", fileId);
  } catch (error) {
    console.error("上传出错:", error);
  }
});
```

##### 暂停和恢复上传

```typescript
let uploader: GridFSResumableUploader | null = null;

// 开始上传
document.getElementById("startBtn")?.addEventListener("click", async () => {
  const file = fileInput.files?.[0];
  if (!file) return;

  uploader = new GridFSResumableUploader(file, {
    uploadUrl: "/api/gridfsupload",
    onProgress: (progress) => {
      console.log(`进度: ${progress.percentage}%`);
    },
  });

  await uploader.start();
});

// 暂停上传
document.getElementById("pauseBtn")?.addEventListener("click", () => {
  uploader?.pause();
});

// 恢复上传
document.getElementById("resumeBtn")?.addEventListener("click", async () => {
  await uploader?.resume();
});

// 取消上传
document.getElementById("cancelBtn")?.addEventListener("click", async () => {
  await uploader?.cancel();
});
```

##### HTML 示例

```html
<!DOCTYPE html>
<html>
  <head>
    <title>GridFS 断点续传示例</title>
  </head>
  <body>
    <h1>文件上传 (支持断点续传)</h1>

    <input type="file" id="fileInput" />
    <div>
      <button id="startBtn">开始上传</button>
      <button id="pauseBtn">暂停</button>
      <button id="resumeBtn">恢复</button>
      <button id="cancelBtn">取消</button>
    </div>

    <progress id="progress" max="100" value="0"></progress>
    <div id="status"></div>

    <script type="module" src="./app.ts"></script>
  </body>
</html>
```

### 🗑️ 文件清理管理

提供完善的文件清理方案,包括过期文件删除、孤立块清理和存储统计。

#### 基础清理操作

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;

var cleanupHelper = new GridFSCleanupHelper(bucket);

// 1. 删除 30 天前的旧文件
var deletedCount = await cleanupHelper.DeleteOldFilesAsync(
    days: 30,
    filePattern: "temp_.*", // 可选: 只删除临时文件
    cancellationToken: cancellationToken
);
Console.WriteLine($"已删除 {deletedCount} 个过期文件");

// 2. 根据元数据删除文件
var count = await cleanupHelper.DeleteByMetadataAsync(
    "category",
    "temporary",
    cancellationToken
);
Console.WriteLine($"已删除 {count} 个临时文件");

// 3. 清理孤立的块 (上传失败遗留的块)
var orphanedChunks = await cleanupHelper.CleanupOrphanedChunksAsync(cancellationToken);
Console.WriteLine($"已清理 {orphanedChunks} 个孤立块");
```

#### 获取存储统计

```csharp
var stats = await cleanupHelper.GetStorageStatsAsync();

Console.WriteLine($"文件总数: {stats.TotalFiles}");
Console.WriteLine($"总大小: {FormatFileSize(stats.TotalSize)}");
Console.WriteLine("\n最大的 10 个文件:");

foreach (var file in stats.LargestFiles)
{
    Console.WriteLine($"  {file.Filename}: {FormatFileSize(file.Size)} (上传于 {file.UploadDate})");
}

static string FormatFileSize(long bytes)
{
    string[] sizes = ["B", "KB", "MB", "GB", "TB"];
    int order = 0;
    double size = bytes;
    while (size >= 1024 && order < sizes.Length - 1)
    {
        order++;
        size /= 1024;
    }
    return $"{size:F2} {sizes[order]}";
}
```

#### 自动清理 - TTL 索引

MongoDB 支持 TTL (Time To Live) 索引自动删除过期文件。

```csharp
// 方式 1: 基于上传时间 - 自动删除 7 天前的文件
await cleanupHelper.CreateTTLIndexAsync(
    expireAfterSeconds: 7 * 24 * 60 * 60, // 7 天
    cancellationToken: cancellationToken
);

// 方式 2: 基于自定义元数据字段
// 首先在上传时设置过期时间
var fileId = await bucket.UploadFromStreamAsync(
    "temp-file.dat",
    stream,
    new GridFSUploadOptions
    {
        Metadata = new BsonDocument
        {
            { "expiresAt", DateTime.UtcNow.AddDays(7) } // 7 天后过期
        }
    }
);

// 然后创建 TTL 索引
await cleanupHelper.CreateTTLIndexAsync(
    expireAfterSeconds: 0, // 到达 expiresAt 时间立即删除
    metadataField: "expiresAt",
    cancellationToken: cancellationToken
);
```

#### 定时清理 - 后台服务

```csharp
using EasilyNET.Mongo.AspNetCore.Helpers;

public class GridFSCleanupBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<GridFSCleanupBackgroundService> _logger;

    public GridFSCleanupBackgroundService(
        IServiceProvider services,
        ILogger<GridFSCleanupBackgroundService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 等待应用启动完成
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var bucket = scope.ServiceProvider.GetRequiredService<IGridFSBucket>();
                var cleanupHelper = new GridFSCleanupHelper(bucket);
                var uploadHelper = new GridFSResumableUploadHelper(bucket);

                // 1. 清理过期上传会话
                await uploadHelper.CleanupExpiredSessionsAsync(stoppingToken);
                _logger.LogInformation("清理过期上传会话完成");

                // 2. 删除 30 天前的临时文件
                var deletedFiles = await cleanupHelper.DeleteOldFilesAsync(
                    days: 30,
                    filePattern: "temp_.*",
                    cancellationToken: stoppingToken
                );
                _logger.LogInformation("删除了 {Count} 个过期临时文件", deletedFiles);

                // 3. 清理孤立块
                var deletedChunks = await cleanupHelper.CleanupOrphanedChunksAsync(stoppingToken);
                _logger.LogInformation("清理了 {Count} 个孤立块", deletedChunks);

                // 4. 记录存储统计
                var stats = await cleanupHelper.GetStorageStatsAsync(stoppingToken);
                _logger.LogInformation(
                    "存储统计 - 文件数: {TotalFiles}, 总大小: {TotalSize} bytes",
                    stats.TotalFiles,
                    stats.TotalSize
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理任务执行失败");
            }

            // 每天凌晨 2 点执行
            var now = DateTime.Now;
            var nextRun = DateTime.Today.AddDays(1).AddHours(2);
            var delay = nextRun - now;

            await Task.Delay(delay, stoppingToken);
        }
    }
}

// 在 Program.cs 中注册
builder.Services.AddHostedService<GridFSCleanupBackgroundService>();
```

#### 清理策略建议

| 文件类型           | 清理策略      | 实现方式                             |
| ------------------ | ------------- | ------------------------------------ |
| 用户上传的永久文件 | 不自动删除    | 无 TTL 索引                          |
| 临时文件/缓存      | 7-30 天后删除 | TTL 索引或定时任务                   |
| 上传失败的残留     | 立即清理      | 后台服务 + CleanupOrphanedChunks     |
| 断点续传会话       | 24 小时后过期 | GridFSResumableUploadHelper 自带 TTL |
| 大文件预览缩略图   | 30 天后删除   | 元数据标记 + 定时任务                |

#### 监控和告警

```csharp
public class GridFSMonitoringService
{
    private readonly GridFSCleanupHelper _cleanupHelper;
    private readonly ILogger<GridFSMonitoringService> _logger;

    public async Task CheckStorageHealthAsync()
    {
        var stats = await _cleanupHelper.GetStorageStatsAsync();

        // 检查存储空间是否超过阈值
        const long maxStorageBytes = 100L * 1024 * 1024 * 1024; // 100GB
        if (stats.TotalSize > maxStorageBytes)
        {
            _logger.LogWarning(
                "GridFS 存储空间即将满! 当前: {Current}GB, 阈值: {Max}GB",
                stats.TotalSize / 1024.0 / 1024 / 1024,
                maxStorageBytes / 1024.0 / 1024 / 1024
            );

            // 发送告警邮件/短信...
        }

        // 检查是否有异常大的文件
        var largeFiles = stats.LargestFiles
            .Where(f => f.Size > 1024L * 1024 * 1024) // > 1GB
            .ToList();

        if (largeFiles.Any())
        {
            _logger.LogInformation(
                "发现 {Count} 个超过 1GB 的大文件",
                largeFiles.Count
            );
        }
    }
}
```

### 🔍 高级用法

#### 块大小优化策略

```csharp
// GridFSUploadHelper 会根据文件大小自动选择最优块大小:
// < 1MB        : 64KB  块 (小文件快速上传)
// 1MB - 10MB   : 255KB 块 (GridFS 默认,通用场景)
// 10MB - 100MB : 512KB 块 (大文件减少块数量)
// >= 100MB     : 1MB   块 (超大文件最优性能)
```

#### 自定义块大小

```csharp
builder.Services.AddMongoGridFS(options =>
{
    // 针对特定场景自定义块大小
    options.ChunkSizeBytes = 512 * 1024; // 512KB

    // 写入策略 - Unacknowledged 提升性能 (不等待写入确认)
    options.WriteConcern = WriteConcern.Unacknowledged;

    // 读取偏好 - Primary 保证数据一致性
    options.ReadPreference = ReadPreference.Primary;
});
```

#### 文件元数据查询

```csharp
// 根据元数据查询文件
var filter = Builders<GridFSFileInfo>.Filter.And(
    Builders<GridFSFileInfo>.Filter.Eq("metadata.userId", "user123"),
    Builders<GridFSFileInfo>.Filter.Eq("metadata.category", "video")
);

var files = await bucket.FindAsync(filter);
await foreach (var file in files.ToAsyncEnumerable())
{
    Console.WriteLine($"文件: {file.Filename}, 大小: {file.Length} bytes");
    Console.WriteLine($"上传时间: {file.UploadDateTime}");

    if (file.Metadata != null)
    {
        Console.WriteLine($"Content-Type: {file.Metadata["contentType"].AsString}");
        Console.WriteLine($"用户 ID: {file.Metadata["userId"].AsString}");
    }
}
```

#### 流式下载 (内存优化)

```csharp
// 直接流式传输,不加载到内存
var fileStream = await bucket.OpenDownloadStreamByNameAsync("large-file.zip");

// 分块读取
var buffer = new byte[8192];
int bytesRead;
while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
{
    // 处理数据块,例如写入响应流
    await Response.Body.WriteAsync(buffer, 0, bytesRead);
}
### 📊 性能对比

| 场景                  | 传统方式           | 优化后          | 提升        |
| --------------------- | ------------------ | --------------- | ----------- |
| 视频播放起始延迟      | 需下载完整文件     | <100ms          | ~100x       |
| 100MB 文件上传        | 1024 块 (100KB/块) | 100 块 (1MB/块) | ~50% faster |
| 批量上传 10 个文件    | 串行处理           | 并行处理        | ~4x faster  |
| 内存占用 (100MB 文件) | ~100MB             | <10MB           | ~90% less   |

### ⚠️ 注意事项

1. **块大小选择**:

   - 小文件(<1MB): 使用较小块(64KB)减少开销
   - 大文件(>100MB): 使用较大块(1MB)减少块数量
   - 流式传输: 推荐 255KB (GridFS 默认)

2. **并行上传**:

   - 根据 CPU 核心数调整并行度
   - 注意数据库连接池大小限制

3. **范围请求**:
   - 确保设置 `Seekable = true`
   - 正确处理 Range 头格式
```
