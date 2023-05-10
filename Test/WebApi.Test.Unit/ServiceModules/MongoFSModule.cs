using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.GridFS;
using EasilyNET.Mongo.GridFS.Extension;
using MongoDB.Driver;

namespace WebApi.Test.Unit;

/// <summary>
/// GridFS
/// </summary>
public class MongoFSModule : AppModule
{
    /// <inheritdoc />
    public MongoFSModule()
    {
        Enable = false;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.AddEasilyNETGridFS(fsOptions: c =>
        {
            c.BusinessApp = "easilyfs";
            c.Options = new()
            {
                BucketName = "easilyfs",
                ChunkSizeBytes = 1024,
                DisableMD5 = true,
                ReadConcern = new(),
                ReadPreference = ReadPreference.Primary,
                WriteConcern = WriteConcern.Unacknowledged
            };
            c.DefaultDB = true;
            c.ItemInfo = "item.info";
        });
    }

    /// <inheritdoc />
    public override void ApplicationInitialization(ApplicationContext context)
    {
        var app = context.GetApplicationBuilder();
        var config = app.ApplicationServices.GetRequiredService<IConfiguration>();
        // 配置虚拟文件
        app.UseGridFSVirtualPath(config);
    }
}