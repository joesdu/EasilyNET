### EasilyNET.Mongo.AspNetCore

- 一个 MongoDB 驱动的服务包,方便使用 MongoDB 数据库.
- 数据库中字段名驼峰命名,ID,Id 自动转化成 ObjectId.
- 可配置部分类的 Id 字段不存为 ObjectId,而存为 string 类型.支持子对象以及集合成员的 Id 字段转化.
- 自动本地化 MongoDB 时间类型
- 添加.Net6 Date/Time Only 类型支持(序列化到 String 或 long)
- 支持通过特性的方式创建和更新索引

---

##### ChangeLogs

- 支持自定义 TimeOnly 和 DateOnly 的格式化格式.
    1. 支持转换成字符串格式
    2. 转换成 Ticks 的方式存储
    3. 若想转化成其他类型也可自行实现,如:转化成 ulong 类型
- 添加动态类型支持[object 和 dynamic], 2.20 版后官方又加上了.
  JsonArray.
- 添加 JsonNode, JsonObject 类型支持.

---

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

#### 使用

- Nuget 安装 EasilyNET.Mongo.AspNetCore
- 在系统环境变量或者 Docker 容器中设置环境变量名称为: CONNECTIONSTRINGS_MONGO = mongodb 链接字符串 或者在
  appsettings.json 中添加,
- 现在你也可以参考 example.api 项目查看直接传入相关数据.
- 添加 APM
  探针支持,根据 [SkyApm.Diagnostics.MongoDB](https://github.com/SkyAPM/SkyAPM-dotnet/tree/main/src/SkyApm.Diagnostics.MongoDB)

```json
{
    "ConnectionStrings": {
        "Mongo": "mongodb链接字符串"
    },
    // 或者使用
    "CONNECTIONSTRINGS_MONGO": "mongodb链接字符串"
}
```

##### 方法 1. 使用默认依赖注入方式

```csharp
var builder = WebApplication.CreateBuilder(args);

// 添加Mongodb数据库服务
builder.Services.AddMongoContext<DbContext>(builder.Configuration, c =>
{
    // 配置数据库名称,覆盖掉连接字符串中的数据库名称
    c.DatabaseName = "test23";
    // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题,所以需要将其调整为字符串.
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
    // 配置自定义Convention
    c.ConventionRegistry= new()
    {
        {
            $"{SnowId.GenerateNewId()}",
            new() { new IgnoreIfDefaultConvention(true) }
        }
    };
    // 通过ClientSettings来配置一些使用特殊的东西
    c.ClientSettings = cs =>
    {
        // 对接 SkyAPM 的 MongoDB探针或者别的事件订阅器
        cs.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventSubscriber());
    };
});
// 添加.NET6+新的TimeOnly和DateOnly数据类型的序列化方案和添加动态类型支持
builder.Services.RegisterSerializer(new DateOnlySerializerAsString());
builder.Services.RegisterSerializer(new TimeOnlySerializerAsString());
// 注册别的序列化方案
builder.Services.RegisterSerializer(new DoubleSerializer(BsonType.Double));
...
var app = builder.Build();
```

##### 方法 2. 使用 EasilyNET.AutoDependencyInjection

- 项目添加 EasilyNET.AutoDependencyInjection Nuget 包
- 创建 EasilyNETMongoModule.cs 并继承 AppModule 类

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

#### 使用 GridFS

- 注册服务

```csharp
// 需要提前注册 IMongoDatabase, 或者使用其他重载来注册服务.
builder.Services.AddMongoGridFS();
```

- 使用依赖注入获取 GridFSBucket 操作 GridFS

```csharp
public class YourClass(IGridFSBucket bucket)
{
    private readonly IGridFSBucket _bucket = bucket;

    public void DoSomething()
    {
        _bucket.XXXXXX();
    }
}
```

#### 使用索引

EasilyNET.Mongo.AspNetCore 支持基于特性自动为实体类创建 MongoDB 索引,且会根据你的字段命名约定(如小驼峰)自动适配索引字段名.

自动索引创建的核心特性：
- 支持在实体属性上使用 [MongoIndex] 特性声明单字段索引.
- 支持在实体类上使用 [MongoCompoundIndex] 特性声明复合索引.
- 支持唯一索引、文本索引、地理空间索引等多种类型.
- 字段名自动适配小驼峰等命名约定,无需手动处理.

使用案例:

```csharp
public class User
{
    [MongoIndex(EIndexType.Ascending, Unique = true)]
    public string UserName { get; set; } = string.Empty;

    [MongoIndex(EIndexType.Descending)]
    public DateTime CreatedAt { get; set; }
}

[MongoCompoundIndex(new[] { "UserName", "CreatedAt" }, new[] { EIndexType.Ascending, EIndexType.Descending }, Unique = true)]
public class Log
{
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

最后在中间件中配置对应DbContext的内容:

```csharp
var app = builder.Build();
// 自动为所有集合创建索引，字段名自动适配小驼峰等命名约定
app.UseCreateMongoIndexes<DbContext>();
// 若存在多个或者集合分布在多个Context中.需要多次应用.
app.UseCreateMongoIndexes<DbContext2>();
```

注意事项：
- 自动索引创建会比对现有索引定义,若定义不一致会自动删除并重建(通过名称匹配,若是不存在对应名称,将不会删除原有索引[为了避免手动创建的索引失效]).