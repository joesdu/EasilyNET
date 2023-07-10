using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.AspNetCore;
using EasilyNET.Mongo.ConsoleDebug;
using EasilyNET.Mongo.Core;
using EasilyNET.MongoSerializer.AspNetCore;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace MongoGridFS.Example;

/// <summary>
/// MongoDB驱动模块
/// </summary>
public class MongoModule : AppModule
{
    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddMongoContext<MongoContext>(new()
        {
            Servers = new List<MongoServerAddress> { new("127.0.0.1", 27018) },
            Credential = MongoCredential.CreateCredential("admin", "guest", "guest"),
            LinqProvider = LinqProvider.V3,
            ClusterConfigurator = s => s.Subscribe(new ActivityEventSubscriber())
        }, c =>
        {
            c.DatabaseName = "test1";
            c.DefaultConventionRegistry = true;
        });
        context.Services.RegisterSerializer(new DateOnlySerializerAsString());
        context.Services.RegisterSerializer(new TimeOnlySerializerAsString());
        context.Services.RegisterDynamicSerializer();
    }
}