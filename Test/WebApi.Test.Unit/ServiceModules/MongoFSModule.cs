using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.MongoGridFS.AspNetCore;
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
        var db = context.Services.GetService<IMongoDatabase>() ?? throw new("请先注册IMongoDatabase服务");
        context.Services.AddMongoGridFS(db);
    }
}