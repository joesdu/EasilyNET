using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Extensions;
using EasilyNET.AutoDependencyInjection.Modules;
using EasilyNET.MongoGridFS.AspNetCore;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using MongoDB.Driver;

namespace MongoGridFS.Example;

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
        context.Services.Configure<FormOptions>(c =>
               {
                   c.MultipartHeadersLengthLimit = int.MaxValue;
                   c.MultipartBodyLengthLimit = long.MaxValue;
                   c.ValueLengthLimit = int.MaxValue;
               })
               .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
               .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
        context.Services.AddMongoGridFS(db);
    }
}