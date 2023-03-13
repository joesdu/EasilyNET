using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo;
using EasilyNET.Mongo.ConsoleDebug;
using EasilyNET.Mongo.Extension;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace WebApi.Test.Unit;

/// <summary>
/// MongoDB驱动模块
/// </summary>
public class MongoModule : AppModule
{
    /// <summary>
    /// 配置和注册服务
    /// </summary>
    /// <param name="context"></param>
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        var config = context.Services.GetConfiguration();

        // 使用 IConfiguration 的方式注册例子,使用链接字符串,仅需将config替换成连接字符即可.
        //context.Services
        //    .AddMongoContext<DbContext>(config, c =>
        //    {
        //        c.Options = op =>
        //        {
        //            op.ObjectIdToStringTypes.AddRange(new[] { typeof(MongoTest2) });
        //            op.DefaultConventionRegistry = true;
        //        };
        //        //c.LinqProvider = MongoDB.Driver.Linq.LinqProvider.V2;
        //    })
        //    .AddMongoContext<DbContext2>(config)
        //    // 添加Guid序列化.但是不加竟然也可以正常工作.
        //    //.RegisterHoyoSerializer(new GuidSerializer(GuidRepresentation.Standard))
        //    .RegisterHoyoSerializer();
        context.Services
               .AddMongoContext<DbContext>(new MongoClientSettings
               {
                   Servers = new List<MongoServerAddress> { new("192.168.2.17", 27017) },
                   Credential = MongoCredential.CreateCredential("admin", "oneblogs", "&oneblogs789"),
                   // 新版驱动使用V3版本,有可能会出现一些Linq表达式客户端函数无法执行,需要调整代码,但是工作量太大了,所以可以先使用V2兼容.
                   LinqProvider = LinqProvider.V3
                   // 对接 SkyAPM 的 MongoDB探针
                   //ClusterConfigurator = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber())
               }, c =>
               {
                   c.DatabaseName = "test";
                   c.Options = op =>
                   {
                       // 配置不需要将Id字段存储为ObjectID的类型.使用$unwind操作符的时候,ObjectId在转换上会有一些问题.
                       op.ObjectIdToStringTypes = new() { typeof(MongoTest2) };
                       // 是否使用HoyoMongo的一些默认转换配置.包含如下内容:
                       // 1.小驼峰字段名称 如: pageSize ,linkPhone 
                       // 2.忽略代码中未定义的字段
                       // 3.将ObjectID字段 _id 映射到实体中的ID或者Id字段,反之亦然.在存入数据的时候将Id或者ID映射为 _id
                       // 4.将枚举类型存储为字符串, 如: Gender.男 存储到数据中为 男,而不是 int 类型
                       op.DefaultConventionRegistry = true;
                   };
                   // HoyoMongoParams.Options 中的 LinqProvider, ClusterBuilder
                   // 会覆盖 MongoClientSettings 中的 LinqProvider 和 ClusterConfigurator 的值,
                   // 所以使用MongoClientSettings注册服务时,可仅赋值其中一个
                   c.LinqProvider = LinqProvider.V3;
                   //c.ClusterBuilder = cb => cb.Subscribe(new DiagnosticsActivityEventSubscriber());
                   c.ClusterBuilder = cb => cb.Subscribe(new ActivityEventSubscriber());
               })
               // DbContext2 由于没有配置 LinqProvider 所以默认为V3版本,
               // ClusterBuilder 也没有配置,所以使用 SkyAPM 也无法捕获到 Context2 的信息
               .AddMongoContext<DbContext2>(config)
               // 添加Guid序列化.但是不加竟然也可以正常工作.
               //.RegisterHoyoSerializer(new GuidSerializer(GuidRepresentation.Standard))
               .RegisterHoyoSerializer();
    }
}