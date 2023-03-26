using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.GridFS;
using MongoDB.Driver;

namespace WebApi.Test.Unit;

/// <summary>
/// GridFS
/// </summary>
public class MongoGridFSModule : AppModule
{
    /// <summary>
    /// 配置服务
    /// </summary>
    /// <param name="context"></param>
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
}