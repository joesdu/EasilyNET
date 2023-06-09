### EasilyNET.Mongo

- 一个 MongoDB 驱动的服务包,方便使用 MongoDB 数据库.
- 数据库中字段名驼峰命名,ID,Id 自动转化成 ObjectId.
- 可配置部分类的 Id 字段不存为 ObjectId,而存为 string 类型.
- 自动本地化 MongoDB 时间类型
- 添加.Net6 Date/Time Only 类型支持(TimeOnly 理论上应该是兼容原 TimeSpan 数据类型).
- Date/Time Only 类型可结合[Hoyo.WebCore](https://github.com/joesdu/Hoyo.WebCore)使用,前端可直接传字符串类型的 Date/Time Only 的值.
- 添加 SkyWalking-APM 探针支持,未依赖 Agent,所以需要手动传入参数.

---

#### 使用

- Nuget 安装 EasilyNET.Mongo
- 推荐同时安装 EasilyNET.Mongo.Extension 包,添加了对 .Net6+ 的 Date/Time Only 类型
- 在系统环境变量或者 Docker 容器中设置环境变量名称为: CONNECTIONSTRINGS_MONGO = mongodb 链接字符串 或者在 appsettings.json 中添加,
- 现在你也可以参考 example.api 项目查看直接传入相关数据.
- 添加 APM 探针支持,根据 [SkyApm.Diagnostics.MongoDB](https://github.com/SkyAPM/SkyAPM-dotnet/tree/main/src/SkyApm.Diagnostics.MongoDB)

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

var provider = builder.Services.BuildServiceProviderFromFactory();
// 添加Mongodb数据库服务
builder.Services.AddMongoContext<DbContext>(provider, builder.Configuration, c =>
{
    c.Options = op =>
    {
        op.AppendConventionRegistry(new()
        {
            {
                "IdentityServer Mongo Conventions",
                new() {new IgnoreIfDefaultConvention(true)}
            }
        });
    };
    // 目前主要是用于 SkyAPM 使用,或者接入我们自己Mongo.ConsoleDebug
    c.ClusterBuilder = op =>
    {
         op.Subscribe(new DiagnosticsActivityEventSubscriber();
         op.Subscribe(new ActivityEventSubscriber());
    });
    // 当使用IConfiguration或者ConnectingString的时候,该配置不生效,因为这两个其实都是使用ConnectingString的方式,可以从连接字符串中获取数据库名称.
    // 使用MonogoClientSettings使用该字段配置数据库名.
    // 若是都未设置将使用本库默认数据库名称.
    c.DatabaseName = "easilynet";
    // 新版的MongoDB驱动使用了 Linq3 的模式,所以原有的程序会出现一些问题,为了避免大改.可以调整为V2,默认为V3
    // 若是使用MongoClientSettings配置的话,该参数不生效,将使用MongoClientSettings中的LinqProvider版本.
    c.LinqProvider = LinqProvider.V2;
    // 传递DbContext构造函数的参数.
    //c.ContextParams = new() { "DbContext测试参数", 1, obj1, ... };
}).RegisterSerializer();
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
        var provider = context.Services.BuildServiceProviderFromFactory();
        // 使用 IConfiguration 的方式注册例子,使用链接字符串,仅需将config替换成连接字符即可.
        //context.Services.AddMongoContext<DbContext>(provider, config, c =>
        //    {
        //        c.Options = op =>
        //        {
        //            op.ObjIdToStringTypes = new() { typeof(MongoTest2) };
        //            op.DefaultConventionRegistry = true;
        //        };
        //        //c.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
        //        // 传递DbContext构造函数的参数.
        //        //c.ContextParams = new() { "DbContext测试参数" };
        //    })
        //    .AddMongoContext<DbContext2>(config)
        //    //.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard))
        //    .RegisterSerializer();

        context.Services.AddMongoContext<DbContext>(provider, new MongoClientSettings
            {
                Servers = new List<MongoServerAddress>
                {
                    new("192.168.2.17",27017),
                    new("192.168.2.18",27017),
                    new("192.168.2.19",27017)
                },
                Credential = MongoCredential.CreateCredential("admin", "oneblogs", "&oneblogs789"),
                // 新版驱动使用V3版本,有可能会出现一些Linq表达式客户端函数无法执行,需要调整代码,但是工作量太大了,所以可以先使用V2兼容.
                LinqProvider = MongoDB.Driver.Linq.LinqProvider.V3,
                // 对接 SkyAPM 的 MongoDB探针
                //ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber())
            }, c =>
            {
                c.DatabaseName = "test";
                c.Options = op =>
                {
                    // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题.
                    op.ObjIdToStringTypes = new() { typeof(MongoTest2) };
                    // 是否使用HoyoMongo的一些默认转换配置.包含如下内容:
                    // 1.小驼峰字段名称 如: pageSize ,linkPhone
                    // 2.忽略代码中未定义的字段
                    // 3.将ObjectID字段 _id 映射到实体中的ID或者Id字段,反之亦然.在存入数据的时候将Id或者ID映射为 _id
                    // 4.将枚举类型存储为字符串, 如: Gender.男 存储到数据中为 男,而不是 int 类型
                    op.DefaultConventionRegistry = true;
                };
                // EasilyNETMongoParams.Options 中的 LinqProvider, ClusterBuilder
                // 会覆盖 MongoClientSettings 中的 LinqProvider 和 ClusterConfigurator 的值,
                // 所以使用MongoClientSettings注册服务时,可仅赋值其中一个
                c.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
                //c.ClusterBuilder = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                // 传递DbContext构造函数的参数.
                //c.ContextParams = new() { "DbContext测试参数" };
            })
            // DbContext2 由于没有配置 LinqProvider 所以默认为V3版本,
            // ClusterBuilder 也没有配置,所以使用 SkyAPM 也无法捕获到 Context2 的信息
            .AddMongoContext<DbContext2>(provider, config)
            // 添加Guid序列化.但是不加竟然也可以正常工作.
            //.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard))
            .RegisterSerializer();
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
