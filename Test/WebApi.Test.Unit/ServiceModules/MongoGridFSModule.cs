using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.Mongo.GridFS;
using EasilyNET.Mongo.GridFS.Extension;
using Microsoft.Extensions.FileProviders;
using MongoDB.Driver;

namespace WebApi.Test.Unit;

/// <summary>
/// GridFS
/// </summary>
public class MongoGridFSModule : AppModule
{
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
        var setting = config.GetSection(EasilyFSSettings.Position).Get<EasilyFSSettings>() ?? throw new("未找到虚拟文件设置");
        if (!Directory.Exists(setting.PhysicalPath)) _ = Directory.CreateDirectory(setting.PhysicalPath);
        _ = app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(setting.PhysicalPath),
            RequestPath = setting.VirtualPath
        });
    }
}