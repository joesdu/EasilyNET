using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.AspNetCore.Serializers;
using EasilyNET.Mongo.ConsoleDebug.Subscribers;
using WebApi.Test.Unit.Common;

namespace WebApi.Test.Unit.ServiceModules;

/// <summary>
/// MongoDB驱动模块
/// </summary>
internal sealed class MongoModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();
        // MongoDB服务初始化完整例子
        //context.Services.AddMongoContext<DbContext>(new MongoClientSettings
        //{
        //    Servers = new List<MongoServerAddress> { new("127.0.0.1", 27018) },
        //    Credential = MongoCredential.CreateCredential("admin", "guest", "guest"),
        //    // 对接 SkyAPM 的 MongoDB探针
        //    ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber())
        //}, c =>
        //{
        //    // 配置数据库名称,覆盖掉连接字符串中的数据库名称
        //    c.DatabaseName = "test23";
        //    // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题,所以需要将其调整为字符串.
        //    c.ObjectIdToStringTypes = new()
        //    {
        //        typeof(MongoTest2)
        //    };
        //    // 是否使用HoyoMongo的一些默认转换配置.包含如下内容:
        //    // 1.小驼峰字段名称 如: pageSize ,linkPhone 
        //    // 2.忽略代码中未定义的字段
        //    // 3.将ObjectID字段 _id 映射到实体中的ID或者Id字段,反之亦然.在存入数据的时候将Id或者ID映射为 _id
        //    // 4.将枚举类型存储为字符串, 如: Gender.男 存储到数据中为 男,而不是 int 类型
        //    c.DefaultConventionRegistry = true;
        //    c.ConventionRegistry = new()
        //    {
        //        {
        //            $"{SnowId.GenerateNewId()}",
        //            new() { new IgnoreIfDefaultConvention(true) }
        //        }
        //    };
        //}).AddMongoContext<DbContext2>(config, c =>
        //{
        //    c.DefaultConventionRegistry = true;
        //    c.ConventionRegistry = new()
        //    {
        //        {
        //            $"{SnowId.GenerateNewId()}",
        //            new() { new IgnoreIfDefaultConvention(true) }
        //        }
        //    };
        //    c.ClientSettings = cs =>
        //    {
        //        cs.ClusterConfigurator = cb => cb.Subscribe(new ActivityEventSubscriber());
        //    };
        //});
        var env = context.ServiceProvider?.GetRequiredService<IWebHostEnvironment>() ?? throw new("获取服务出错");
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
                            Enable = true
                        }));
                    }
                    s.Subscribe(new ActivityEventDiagnosticsSubscriber(new()
                    {
                        CaptureCommandText = true
                    }));
                };
                cs.ApplicationName = Constant.InstanceName;
                // https://www.mongodb.com/docs/drivers/csharp/current/fundamentals/logging/#log-messages-by-category
                //cs.LoggingSettings = new(LoggerFactory.Create(b =>
                //{
                //    b.AddConfiguration(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
                //    {
                //        //{ "LogLevel:Default", "Debug" },
                //        { "LogLevel:MongoDB.COMMAND", "Debug" }
                //        //{ "LogLevel:MongoDB.CONNECTION", "Debug" },
                //        //{ "LogLevel:MongoDB.INTERNAL.*", "Debug" },
                //        //{ "LogLevel:MongoDB.SERVERSELECTION", "Debug" }
                //    }).Build());
                //    b.AddSimpleConsole();
                //}));
            };
        });
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());
        context.Services.RegisterSerializer(new JsonNodeSerializer());
        context.Services.RegisterDynamicSerializer();
    }
}