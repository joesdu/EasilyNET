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

GridFS 是 MongoDB 的分布式文件系统，支持存储超过 16MB 的文件。

### 基础使用

1. **注册服务**:

```csharp
// 需要提前注册 IMongoDatabase，或使用其他重载
builder.Services.AddMongoGridFS();
```

2. **依赖注入使用**:

````csharp
public class FileService(IGridFSBucket bucket)
{
    private readonly IGridFSBucket _bucket = bucket;

    public async Task UploadFileAsync(Stream stream, string filename)
    {
        var id = await _bucket.UploadFromStreamAsync(filename, stream);
        return id;
    }

    public async Task<Stream> DownloadFileAsync(string filename)
    {
        return await _bucket.OpenDownloadStreamByNameAsync(filename);
    }
}

---

## 🏷️ 索引管理

EasilyNET.Mongo.AspNetCore 支持基于特性自动为实体类创建 MongoDB 索引，会根据字段命名约定（如小驼峰）自动适配索引字段名。

### 核心特性

- **单字段索引**: 使用 `[MongoIndex]` 特性声明
- **复合索引**: 使用 `[MongoCompoundIndex]` 特性声明
- **索引类型**: 支持唯一索引、文本索引、地理空间索引等
- **自动适配**: 字段名自动适配命名约定

### 使用示例

```csharp
public class User
{
    [MongoIndex(EIndexType.Ascending, Unique = true)]
    public string UserName { get; set; } = string.Empty;

    [MongoIndex(EIndexType.Descending)]
    public DateTime CreatedAt { get; set; }
}

[MongoCompoundIndex(
    new[] { "UserName", "CreatedAt" },
    new[] { EIndexType.Ascending, EIndexType.Descending },
    Unique = true)]
public class Log
{
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### 配置索引创建

```csharp
var app = builder.Build();

// 自动为所有集合创建索引，字段名自动适配命名约定
app.UseCreateMongoIndexes<DbContext>();

// 若存在多个 DbContext，需要多次应用
app.UseCreateMongoIndexes<DbContext2>();
```

### 注意事项

- 自动索引创建会比对现有索引定义
- 若定义不一致会自动删除并重建（通过名称匹配）
- 若不存在对应名称，不会删除原有索引（避免手动创建的索引失效）

---

## 📚 更多资源

- [示例项目](https://github.com/joesdu/EasilyNET/tree/main/sample)
- [API 文档](https://github.com/joesdu/EasilyNET/wiki)
- [问题反馈](https://github.com/joesdu/EasilyNET/issues)

---

_最后更新: 2025 年 9 月 3 日_
