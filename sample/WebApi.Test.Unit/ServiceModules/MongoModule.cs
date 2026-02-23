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
        var config = context.Configuration;
        // 全局 Convention 配置（可选，最多调用一次，必须在 AddMongoContext 之前）
        // 调用后仅使用用户自定义的约定，本库内置默认约定不会被应用
        // 若不调用，首次 AddMongoContext 时将自动使用默认配置（驼峰命名 + 忽略未知字段 + _id 映射 + 枚举存字符串）
        //context.Services.ConfigureMongoConventions(c =>
        //{
        //    // 配置不需要将Id字段存储为ObjectID的类型,会存储为字符串类型
        //    c.ObjectIdToStringTypes = [typeof(MongoTest2)];
        //    // 添加自定义约定包（支持链式调用）
        //    c.AddConvention("myConvention", new() { new CamelCaseElementNameConvention() })
        //     .AddConvention("ignoreDefault", new() { new IgnoreIfDefaultConvention(true) });
        //});
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
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());
        context.Services.RegisterSerializer(new JsonNodeSerializer());
        context.Services.RegisterSerializer(new JsonObjectSerializer());
        context.Services.RegisterDynamicSerializer();
        context.Services.RegisterGlobalEnumKeyDictionarySerializer();
    }

    public override async Task ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationHost() as IApplicationBuilder;
        app?.UseCreateMongoIndexes<DbContext>();
        app?.UseCreateMongoIndexes<DbContext2>();
        await base.ApplicationInitialization(context);
    }
}