using EasilyNET.AutoDependencyInjection.Contexts;
using EasilyNET.AutoDependencyInjection.Modules;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace WebApi.Test.Unit;

/// <summary>
/// GridFS
/// </summary>
public class MongoFSModule : AppModule
{
    /// <inheritdoc />
    public MongoFSModule()
    {
        Enable = true;
    }

    /// <inheritdoc />
    public override void ConfigureServices(ConfigureServicesContext context)
    {
        context.Services.Configure<FormOptions>(c =>
               {
                   c.MultipartHeadersLengthLimit = int.MaxValue;
                   c.MultipartBodyLengthLimit = long.MaxValue;
                   c.ValueLengthLimit = int.MaxValue;
               })
               .Configure<KestrelServerOptions>(c => c.Limits.MaxRequestBodySize = null)
               .Configure<IISServerOptions>(c => c.MaxRequestBodySize = null);
        context.Services.AddMongoGridFS();
    }
}