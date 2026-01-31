### EasilyNET.Mongo.AspNetCore

一个强大的 MongoDB 驱动服务包，为 ASP.NET Core 应用提供便捷的 MongoDB 数据库操作支持。

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

#### 5. **APM 监控支持**

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

### **常见问题排查**

#### MongoConnectionPoolPausedException: "The connection pool is in paused state"

出现该异常通常意味着驱动已将目标服务器标记为不可用并暂停连接池，常见原因与解决方式如下：

- **网络不可达/防火墙拦截**：确认应用所在机器能访问 `host:port`，安全组/防火墙已放行。
- **认证或 TLS 配置错误**：确认用户名、密码、`authSource`、`tls/ssl` 参数正确。
- **单节点/代理访问**：若只暴露单节点或通过负载均衡代理访问，请在连接串中添加 `directConnection=true` 或 `loadBalanced=true`。
- **副本集名称不匹配**：连接串中的 `replicaSet` 必须与服务端一致。
- **连接池激增导致服务端拒绝**：避免在客户端强制设置过大的 `MinConnectionPoolSize`，必要时降低并发或限制 `MaxConnectionPoolSize`。

推荐在连接串中显式设置超时，避免长时间阻塞：

```
mongodb://user:pwd@host:27017/db?serverSelectionTimeoutMS=5000&connectTimeoutMS=5000&socketTimeoutMS=30000
```

若问题仍持续，请开启驱动日志（`MongoDB.SERVERSELECTION` / `MongoDB.CONNECTION`）以定位具体原因。

### **MongoDB Context 配置**

#### 弹性与自动恢复（推荐）

MongoDB 驱动自带连接自动恢复机制，配合合理的超时与连接池配置可显著降低“连接池暂停”等问题出现概率。
本库通过 `Resilience.Enable` 提供开箱即用的弹性默认值（符合 MongoDB 官方推荐），这些设置与驱动内置的自动恢复机制协同工作，你可以按需启用和调整：

```csharp
builder.Services.AddMongoContext<DbContext>(builder.Configuration, c =>
{
    // 启用弹性默认配置（与驱动内置自动恢复机制配合使用）
    c.Resilience.Enable = true;

    // 可选：针对特定场景调整（以下为默认值）
    c.Resilience.ServerSelectionTimeout = TimeSpan.FromSeconds(10);  // 服务器选择超时
    c.Resilience.ConnectTimeout = TimeSpan.FromSeconds(10);          // TCP 连接建立超时
    c.Resilience.SocketTimeout = TimeSpan.FromSeconds(60);           // Socket 读写超时
    c.Resilience.WaitQueueTimeout = TimeSpan.FromMinutes(1);         // 连接池等待超时
    c.Resilience.HeartbeatInterval = TimeSpan.FromSeconds(10);       // 心跳检测间隔
    c.Resilience.MaxConnectionPoolSize = 100;                        // 连接池最大连接数
    c.Resilience.MinConnectionPoolSize = null;                       // 连接池最小连接数（null = 使用驱动默认值，不覆盖驱动默认配置）
    c.Resilience.RetryReads = true;                                  // 自动重试读操作
    c.Resilience.RetryWrites = true;                                 // 自动重试写操作
});
```

**场景化调整建议**：

1. **低延迟网络（同区域部署）**：
   - `ConnectTimeout = 5s`, `ServerSelectionTimeout = 5s` 可以更快速失败
   - 适合微服务间通信

2. **跨区域/高延迟网络**：
   - 保持默认值或适当提高：`ConnectTimeout = 20s`, `ServerSelectionTimeout = 30s`
   - 适合多地域分布式部署

3. **高并发/连接池瓶颈场景**：
   - 提高 `MaxConnectionPoolSize = 200` 或更高
   - 降低 `WaitQueueTimeout = 30s` 快速失败，避免雪崩

4. **代理/单节点访问**：
   - 必须在连接串中添加 `directConnection=true` 或 `loadBalanced=true`
   - 示例：`mongodb://user:pwd@host:27017/db?directConnection=true`

5. **Atlas/云托管 MongoDB**：
   - 保持默认值即可，云服务已优化连接行为
   - 确保开启 `RetryReads` 和 `RetryWrites`

> ⚠️ **重要**：弹性配置不会自动解决网络/认证/拓扑配置问题，请先确认连接串与服务端配置一致。

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
